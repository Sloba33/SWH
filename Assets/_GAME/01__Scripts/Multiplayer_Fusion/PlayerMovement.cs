using UnityEngine;
using Fusion;
// Removed using System.ComponentModel; as it's likely not needed

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    private Player _player;
    private Rigidbody _rb;
    // REMOVED: private Animator _anim; // Animator reference is moved to PlayerAnimation
    private CapsuleCollider _playerCollider;
    private RigidbodyConstraints _originalConstraints; // Store original constraints
    private PlayerAnimation _playerAnimation; // New reference to PlayerAnimation

    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private MovementType _movementType; // Your existing enum
    public float _jumpForce = 5.8f;
    [SerializeField] private ParticleSystem _jumpParticle; // Assign in inspector

    [SerializeField] float jumpCoyoteTime = 0.14f; // Duration for the coyote time

    [Networked] TickTimer _coyoteTimer { get; set; }

    [Header("Grounding Detection")]
    [SerializeField] private LayerMask _groundMask;
    private Vector3 _grounderOffset = new Vector3(0, -0.47f, 0.01f);
    private float _grounderRadius = 0.1f;
    [Networked] public NetworkBool IsGrounded { get; set; } // Networked state
    [Networked] public NetworkBool IsJumping { get; set; } // Networked state
    [Networked] public NetworkBool RecentlyJumped { get; set; } // Networked state (for coyote time)
    [Networked] public NetworkBool IsFalling { get; set; } // Networked state

    [Header("Obstacle Detection")]
    [SerializeField] public LayerMask _obstacleMask;
    [SerializeField] public LayerMask _bombObstacleMask;
    [SerializeField] public LayerMask _tileObstacleMask;
    private float _wallCheckOffset = 0.45f;
    private float _bombCheckOffset = 0.9f;
    private float _wallCheckRadius = 0.12f;
    private float _bombCheckRadius = 0.1f;
    [HideInInspector] public Vector3 WallDetectionOffset = new Vector3(0, -0.12f, 0);
    [HideInInspector] public Vector3 BombDetectionOffset = new Vector3(0, -0.12f, 0);
    [Networked] public NetworkBool _isAgainstWall { get; set; }
    [Networked] public NetworkBool _isBombBlocked { get; set; }

    [Header("Raycast Settings")]
    public float raycastDistance = 1.0f;
    public float avoidanceMultiplier;
    public float raycastAngle;
    [HideInInspector] public Vector3 leftEyeOffset = new Vector3(0.025f, 0, 0);
    [HideInInspector] public Vector3 rightEyeOffset = new Vector3(-0.025f, 0, 0);
    private Vector3 _leftEyeDirection, _rightEyeDirection;

    // Internal state for movement
    // Make this networked if precise movement direction is critical for animation or other networked logic
    [Networked] public Vector3 _currentMoveDirection { get; set; }
    private bool _canMove = true; // This might be controlled by other components (e.g., PlayerInteraction)

    [Header("Tile Detection")]
    [SerializeField] public LayerMask _tileLayer; // Make sure this is assigned in inspector
    [HideInInspector] public float tileRayLength = 50f;
    [Networked] public Tile CurrentTile { get; set; } // Networked if other clients need to know player's current tile

    // Slide Flags
    private float slideCheckerRadius = 0.025f;
    private float frontoffs = -0.3f;
    private float backoffs = -0.3f;
    [HideInInspector] public Vector3 forwardOffset = new Vector3(0, 0.48f, 0);
    [HideInInspector] public Vector3 backOffset = new Vector3(0, 0.48f, 0);
    private float slideForce = 65f;
    private float slideAngle = 25f;


    public void Initialize(Player player, Rigidbody rb, CapsuleCollider collider)
    {
        _player = player;
        _rb = rb;
        _playerCollider = collider;
        _originalConstraints = _rb.constraints;

        // Get PlayerAnimation component (assuming it's on the same GameObject)
        _playerAnimation = GetComponent<PlayerAnimation>();
        if (_playerAnimation == null)
        {
            Debug.LogError("PlayerMovement: PlayerAnimation component not found on this GameObject.");
        }

        // Initialize networked properties
        IsGrounded = false;
        IsJumping = false;
        RecentlyJumped = false;
        IsFalling = false;
        _isAgainstWall = false;
        _isBombBlocked = false;
        CurrentTile = null;
        _currentMoveDirection = Vector3.zero; // Initialize networked property
    }

    public override void FixedUpdateNetwork()
    {
        // Update the current tile every tick
        UpdateCurrentTile();

        // Check coyote timer
        if (_coyoteTimer.Expired(Runner))
        {
            RecentlyJumped = false;
            _coyoteTimer = TickTimer.None; // Reset timer once expired
        }

        // Handle ground status
        HandleGrounded();

        // Handle fall status
        Fall();

        // Handle slide flags (if needed in FixedUpdateNetwork for deterministic physics)
        SlideFlags();

        // Input handling and movement application logic should be called from PlayerController's FixedUpdateNetwork
    }

    public void HandleMovement(Vector3 inputMoveDirection, NetworkBool jumpInput)
    {
        if (!_canMove) return;

        // Update direction based on input
        if (inputMoveDirection != Vector3.zero)
        {
            transform.forward = inputMoveDirection;
            _currentMoveDirection = inputMoveDirection; // Update networked move direction
        }
        else
        {
            _currentMoveDirection = Vector3.zero; // Ensure it's zero when no input
        }

        Vector3 avoidanceVector = Vector3.zero;
        bool avoidObstacle = false;

        // Perform raycasts for obstacle avoidance
        _leftEyeDirection = Quaternion.Euler(0, -raycastAngle, 0) * transform.forward;
        _rightEyeDirection = Quaternion.Euler(0, raycastAngle, 0) * transform.forward;

        bool _leftEyeHitObstacle = Physics.Raycast(
            transform.position + leftEyeOffset,
            _leftEyeDirection,
            out RaycastHit leftEyeHit,
            raycastDistance,
            _tileObstacleMask
        );
        bool _rightEyeHitObstacle = Physics.Raycast(
            transform.position + rightEyeOffset,
            _rightEyeDirection,
            out RaycastHit rightEyeHit,
            raycastDistance,
            _tileObstacleMask
        );

        if (_leftEyeHitObstacle && _rightEyeHitObstacle)
        {
            Vector3 leftAvoidanceVector = Vector3.Cross(leftEyeHit.normal, Vector3.up).normalized;
            Vector3 rightAvoidanceVector = Vector3.Cross(rightEyeHit.normal, Vector3.up).normalized;
            avoidanceVector = (leftAvoidanceVector + rightAvoidanceVector).normalized;
            avoidObstacle = true;
        }
        else if (_leftEyeHitObstacle)
        {
            avoidanceVector = Vector3.Cross(leftEyeHit.normal, Vector3.up).normalized;
            avoidObstacle = true;
        }
        else if (_rightEyeHitObstacle)
        {
            avoidanceVector = Vector3.Cross(rightEyeHit.normal, Vector3.up).normalized;
            avoidObstacle = true;
        }

        Vector3 finalMovement = _currentMoveDirection;
        if (avoidObstacle && !IsFalling && _currentMoveDirection != Vector3.zero)
        {
            finalMovement = _currentMoveDirection + avoidanceVector * avoidanceMultiplier;
        }

        if (_rb != null)
        {
            Vector3 targetVelocity = finalMovement * _player.MoveSpeed;
            _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
        }

        // NO Animator calls here!

        // Handle jump input
        if (jumpInput && IsGrounded && !IsJumping)
        {
            Jump();
        }
    }

    public void HandleGrounded()
    {
        bool newGrounded = Physics.OverlapSphereNonAlloc(
            transform.position + _grounderOffset,
            _grounderRadius,
            new Collider[1], // Use a local array here
            _groundMask
        ) > 0;

        if (!IsGrounded && newGrounded)
        {
            IsGrounded = true;
            // REMOVED: _anim.SetBool("Grounded", true); // PlayerAnimation handles this
            IsFalling = false;
            // REMOVED: _anim.SetBool("Falling", false); // PlayerAnimation handles this
            IsJumping = false; // Player is no longer jumping when they land
            // REMOVED: _anim.SetBool("Jumping", false); // PlayerAnimation handles this
            RecentlyJumped = false; // Reset coyote time
            Friction(true); // Apply friction when grounded
        }
        else if (IsGrounded && !newGrounded)
        {
            IsGrounded = false;
            // REMOVED: _anim.SetBool("Grounded", false); // PlayerAnimation handles this
            if (!RecentlyJumped) // Only set to falling if coyote time has expired or not active
            {
                IsFalling = true;
            }
        }

        // Update wall/bomb blocked states (these could be networked if relevant for gameplay)
        _isAgainstWall = Physics.OverlapSphereNonAlloc(
            WallDetectPosition,
            _wallCheckRadius,
            new Collider[1],
            _obstacleMask
        ) > 0;

        _isBombBlocked = Physics.OverlapSphereNonAlloc(
            BombDetectPosition,
            _bombCheckRadius,
            new Collider[1],
            _bombObstacleMask
        ) > 0;
    }

    public void Fall()
    {
        // Set 'IsFalling' to true only when truly airborne and not currently jumping
        // PlayerAnimation will then react to this state.
        if (!IsGrounded && !IsJumping && _rb.linearVelocity.y < -0.1f) // Added a velocity check for actual downward movement
        {
            IsFalling = true;
            Friction(false); // Remove friction while falling
        }
        // When IsGrounded becomes true, HandleGrounded will set IsFalling to false.
    }

    private void Jump()
    {
        IsJumping = true;
        RecentlyJumped = true; // Mark as recently jumped for coyote time
        _coyoteTimer = TickTimer.CreateFromSeconds(Runner, jumpCoyoteTime);
        _jumpParticle.Play(); // Particle effects are usually visual, fine to keep here.
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, _rb.linearVelocity.z);
        // REMOVED: _anim.SetBool("Jumping", true); // PlayerAnimation handles this based on IsJumping state
        //AudioManager.Instance.PlayJumpSound(_player.characterStats.female, transform.position); // Assuming AudioManager is global
        Friction(false);
    }

    public void Friction(bool mode)
    {
        if (_playerCollider == null || _playerCollider.sharedMaterial == null) return;

        if (mode)
        {
            _playerCollider.sharedMaterial.staticFriction = 1;
            _playerCollider.sharedMaterial.dynamicFriction = 1;
        }
        else
        {
            _playerCollider.sharedMaterial.staticFriction = 0;
            _playerCollider.sharedMaterial.dynamicFriction = 0;
        }
    }

    // Helpers for Gizmos (Editor only)
    // Using _playerCollider.transform instead of _anim.transform for consistency
    private Vector3 WallDetectPosition => (_playerCollider.transform.position - WallDetectionOffset) + _playerCollider.transform.forward * _wallCheckOffset;
    private Vector3 BombDetectPosition => (_playerCollider.transform.position - BombDetectionOffset) + _playerCollider.transform.forward * _bombCheckOffset;

    // Draw Gizmos for debugging in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position + _grounderOffset, _grounderRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(WallDetectPosition, _wallCheckRadius);
        Gizmos.DrawWireSphere(BombDetectPosition, _bombCheckRadius);

        // Draw raycasts
        Debug.DrawRay(transform.position + leftEyeOffset, Quaternion.Euler(0, -raycastAngle, 0) * transform.forward * raycastDistance, Color.blue);
        Debug.DrawRay(transform.position + rightEyeOffset, Quaternion.Euler(0, raycastAngle, 0) * transform.forward * raycastDistance, Color.blue);
    }

    // Using transform.position for these as well
    private Vector3 Front => (transform.position - forwardOffset) + transform.forward * frontoffs;
    private Vector3 Back => (transform.position - backOffset) + transform.forward * backoffs;

    public void SlideFlags()
    {
        bool slideForward = Physics.OverlapSphereNonAlloc(Front, slideCheckerRadius, new Collider[1], _obstacleMask) > 0;
        bool slideBack = Physics.OverlapSphereNonAlloc(Back, slideCheckerRadius, new Collider[1], _obstacleMask) > 0;

        if ((slideForward || slideBack) && !IsGrounded && IsFalling && _currentMoveDirection == Vector3.zero)
        {
            Vector3 slideDirection = Vector3.zero;
            Vector3 forceDirection = Quaternion.Euler(slideAngle, 0f, 0f) * transform.forward;

            if (slideForward) slideDirection += -transform.forward;
            if (slideBack) slideDirection += transform.forward;

            Vector3 totalForce = (slideDirection * slideForce) + (forceDirection * slideForce);
            _rb.AddForce(totalForce, ForceMode.Impulse);
        }
    }
    public void UpdateCurrentTile()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, tileRayLength, _tileLayer))
        {
            Tile hitTile = hit.collider.GetComponent<Tile>();
            if (hitTile != null)
            {
                CurrentTile = hitTile;
            }
            else
            {
                CurrentTile = null;
            }
        }
        else
        {
            CurrentTile = null;
        }
    }

    public Vector3 FindNeighbouringTilePosition()
    {
        if (CurrentTile == null)
        {
            Debug.LogWarning("Player is not on a tile, cannot find neighboring tile.");
            return transform.position + transform.forward;
        }

        Vector3 facingDirection = transform.forward;
        RaycastHit hit;

        if (Physics.Raycast(CurrentTile.transform.position, facingDirection, out hit, raycastDistance, _tileLayer))
        {
            Tile nextTile = hit.collider.GetComponent<Tile>();
            if (nextTile != null)
            {
                return nextTile.transform.position;
            }
            else
            {
                return CurrentTile.transform.position + facingDirection;
            }
        }
        else
        {
            return CurrentTile.transform.position + facingDirection;
        }
    }

    public Tile GetNeighbouringTile()
    {
        if (CurrentTile == null) return null;

        Vector3 facingDirection = transform.forward;
        RaycastHit hit;

        if (Physics.Raycast(CurrentTile.transform.position, facingDirection, out hit, raycastDistance, _tileLayer))
        {
            return hit.collider.GetComponent<Tile>();
        }
        return null;
    }
}