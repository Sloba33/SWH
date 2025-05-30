using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private Player _player; //
    private Rigidbody _rb; //
    private CapsuleCollider _playerCollider; //
    private RigidbodyConstraints _originalConstraints; // Store original constraints
    private PlayerInputHandler _inputHandler; // Reference to the input handler

    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 2f; //
    public float _jumpForce = 5.8f; //
    [SerializeField] private ParticleSystem _jumpParticle; // Assign in inspector


    [Header("Grounding Detection")]
    [SerializeField] private LayerMask _groundMask; //
    private Vector3 _grounderOffset = new Vector3(0, -0.47f, 0.01f); //
    private float _grounderRadius = 0.1f; //
    public bool IsGrounded { get; private set; } // Read-only property for other scripts to check
    public bool IsJumping { get; private set; } // Read-only property
    public bool IsFalling { get; private set; } // Read-only property
    public bool IsPushing { get; set; } = false; // To be set by PlayerController
    public bool IsPulling { get; set; } = false; // To be set by PlayerController
    private bool _jumpInputReceivedThisFrame = false; // Internal flag to handle event in FixedUpdate
    [Header("Obstacle Detection")]
    [SerializeField] public LayerMask _obstacleMask; //
    [SerializeField] public LayerMask _bombObstacleMask; //
    [SerializeField] public LayerMask _tileObstacleMask; //
    private float _wallCheckOffset = 0.45f; //
    private float _bombCheckOffset = 0.9f; //
    private float _wallCheckRadius = 0.12f; //
    private float _bombCheckRadius = 0.1f; //
    [HideInInspector] public Vector3 WallDetectionOffset = new Vector3(0, -0.12f, 0); //
    [HideInInspector] public Vector3 BombDetectionOffset = new Vector3(0, -0.12f, 0); //
    public bool IsAgainstWall { get; private set; } //
    public bool IsBombBlocked { get; private set; } //

    [Header("Raycast Settings")]
    public float raycastDistance = 1.0f; //
    public float avoidanceMultiplier; //
    public float raycastAngle; //
    [HideInInspector] public Vector3 leftEyeOffset = new Vector3(0.025f, 0, 0); //
    [HideInInspector] public Vector3 rightEyeOffset = new Vector3(-0.025f, 0, 0); //

    // Public properties for other scripts to read current state
    public Vector3 CurrentMoveDirection { get; private set; } // The direction player is trying to move in
    public float CurrentCalculatedMoveSpeed { get; private set; } // The actual speed applied after calculations
    [SerializeField]
    public bool CanMove { get; set; } = true; // Control movement from other scripts (e.g., PlayerController)

    [Header("Tile Detection")]
    [SerializeField] public LayerMask _tileLayer; // Make sure this is assigned in inspector
    [HideInInspector] public float tileRayLength = 50f; //
    public Tile CurrentTile { get; private set; } //

    // Slide Flags
    private float slideCheckerRadius = 0.025f; //
    private float frontoffs = 0.3f; // Changed from -0.3f based on PlayerController.cs
    private float backoffs = -0.3f; //
    [HideInInspector] public Vector3 forwardOffset = new Vector3(0, 0.48f, 0); //
    [HideInInspector] public Vector3 backOffset = new Vector3(0, 0.48f, 0); //
    private float slideForce = 65f; //
    private float slideAngle = 25f; //


    public void Initialize(Player player, Rigidbody rb, CapsuleCollider collider)
    {
        _player = player;
        _rb = rb;
        _playerCollider = collider;
        _originalConstraints = _rb.constraints;

        _inputHandler = GetComponent<PlayerInputHandler>();
        if (_inputHandler == null)
        {
            Debug.LogError("PlayerMovement: PlayerInputHandler not found on this GameObject.");
        }

        IsGrounded = false;
        IsJumping = false;
        IsFalling = false;
        IsAgainstWall = false;
        IsBombBlocked = false;
        CurrentTile = null;
        CurrentMoveDirection = Vector3.zero;
        CurrentCalculatedMoveSpeed = 0f;

        GameEvents.OnJumpButtonPressed.AddListener(HandleJumpEvent);
    }

    private void OnDestroy()
    {
        if (GameEvents.OnJumpButtonPressed != null) // Check for null in case script is destroyed before event system
        {
            GameEvents.OnJumpButtonPressed.RemoveListener(HandleJumpEvent);
        }
    }

    private void HandleJumpEvent()
    {
        _jumpInputReceivedThisFrame = true;
    }

    private void FixedUpdate()
    {
        // ... (existing movement logic) ...

        if (!CanMove)
        {
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            CurrentMoveDirection = Vector3.zero;
            CurrentCalculatedMoveSpeed = 0f;
        }
        else
        {
            HandleMovement(_inputHandler.MoveInput);
        }
        HandleGrounded();
        Fall();
        SlideFlags();

        // --- Jump Check in FixedUpdate ---
        // Combine input from keyboard/joystick (via InputHandler) AND mobile button (via internal flag)
        bool jumpInputDetected = (_inputHandler != null && _inputHandler.GetJumpPressedThisFrame()) || _jumpInputReceivedThisFrame;

        // Reset the mobile jump input flag immediately after checking
        // This ensures it's only true for one FixedUpdate tick.
        if (_jumpInputReceivedThisFrame)
        {
            // Debug.Log("Processing UI jump input for this FixedUpdate tick."); // Debugging
            _jumpInputReceivedThisFrame = false;
        }

        if (jumpInputDetected && IsGrounded && !IsJumping && !IsPulling && !IsPushing)
        {
            // Debug.Log("Conditions met for Jump!"); // Debugging
            Jump();
        }
        else if (jumpInputDetected)
        {
            // Debug.Log($"Jump conditions NOT met: Grounded={IsGrounded}, Jumping={IsJumping}, Pulling={IsPulling}, Pushing={IsPushing}"); // Debugging
        }
    }

    private void HandleMovement(Vector2 input)
    {
        if (!CanMove)
            return;
        // Use the already processed 4-directional input
        Vector3 desiredMoveDirection = new Vector3(input.x, 0f, input.y);

        // Update player's forward direction only if there's input
        if (desiredMoveDirection != Vector3.zero)
        {
            transform.forward = desiredMoveDirection;
            CurrentMoveDirection = desiredMoveDirection; // Update public property
        }
        else
        {
            CurrentMoveDirection = Vector3.zero; // No movement input
        }

        Vector3 avoidanceVector = ComputeAvoidanceVector(); // Calculate avoidance
        Vector3 finalMovementDirection = CurrentMoveDirection;

        // Apply avoidance if moving and not falling
        if (CurrentMoveDirection != Vector3.zero && !IsFalling)
        {
            // Only apply avoidance if there's an actual obstacle hit, not just for raycast debug
            bool leftEyeHitObstacle = Physics.Raycast(transform.position + leftEyeOffset, Quaternion.Euler(0, -raycastAngle, 0) * transform.forward, raycastDistance, _tileObstacleMask);
            bool rightEyeHitObstacle = Physics.Raycast(transform.position + rightEyeOffset, Quaternion.Euler(0, raycastAngle, 0) * transform.forward, raycastDistance, _tileObstacleMask);

            if (leftEyeHitObstacle || rightEyeHitObstacle)
            {
                finalMovementDirection = CurrentMoveDirection + avoidanceVector * avoidanceMultiplier;
            }
        }

        // Apply velocity
        CurrentCalculatedMoveSpeed = (_player != null && CurrentMoveDirection != Vector3.zero) ? _player.MoveSpeed : 0f;
        Vector3 targetVelocity = finalMovementDirection * CurrentCalculatedMoveSpeed;
        _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
    }

    private void HandleJumpInput(bool jumpInput)
    {
        if (jumpInput && IsGrounded && !IsJumping)
        {
            Jump(); // Call the Jump method
        }
    }
    public void Jump()
    {
        // Debug.Log("Executing Jump!"); // Debugging
        IsJumping = true;
        IsGrounded = false; // Player is no longer grounded when jumping
        if (_jumpParticle != null) // Safety check
        {
            _jumpParticle.Play();
        }
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, _rb.linearVelocity.z);
        Friction(false);

       
        if (_player != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJumpSound(_player.characterStats.female, transform.position);
        }
    }
    // public void Jump()
    // {
    //     // Only allow jump if currently grounded and not already jumping
    //     if (IsGrounded && !IsJumping)
    //     {
    //         IsJumping = true;
    //         IsGrounded = false; // Player is no longer grounded when jumping
    //         _jumpParticle.Play(); // Play particle effect
    //         _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, _rb.linearVelocity.z); // Apply jump force
    //         Friction(false); // Remove friction during jump

    //         // Play jump sound, assuming AudioManager is a singleton accessible here
    //         // if (_player != null && AudioManager.Instance != null)
    //         // {
    //         //     AudioManager.Instance.PlayJumpSound(_player.characterStats.female, transform.position);
    //         // }
    //     }
    // }

    public bool HandleGrounded()
    {
        // OverlapSphereNonAlloc is more performant than OverlapSphere if used in FixedUpdate
        Collider[] groundHits = new Collider[1]; // Use a small array to reduce garbage
        bool newGrounded = Physics.OverlapSphereNonAlloc(
            transform.position + _grounderOffset,
            _grounderRadius,
            groundHits, // Pass the array to store results
            _groundMask
        ) > 0;

        if (!IsGrounded && newGrounded)
        {
            IsGrounded = true;
            IsFalling = false;
            IsJumping = false; // Reset jumping state when grounded
            Friction(true); // Apply friction when grounded
        }
        else if (IsGrounded && !newGrounded)
        {
            IsGrounded = false;
            // Only set to falling if not jumping (just walked off a ledge)
            if (!IsJumping && _rb.linearVelocity.y < -0.1f) // Added velocity check to ensure actual fall
            {
                IsFalling = true;
            }
        }

        // Update wall/bomb blocked states (these could be networked if relevant for gameplay)
        Collider[] wallHits = new Collider[1];
        IsAgainstWall = Physics.OverlapSphereNonAlloc(
            WallDetectPosition,
            _wallCheckRadius,
            wallHits,
            _obstacleMask
        ) > 0;

        Collider[] bombHits = new Collider[1];
        IsBombBlocked = Physics.OverlapSphereNonAlloc(
            BombDetectPosition,
            _bombCheckRadius,
            bombHits,
            _bombObstacleMask
        ) > 0;
        if (IsAgainstWall && IsJumping)
            CanMove = false;
        return newGrounded;
    }

    public void Fall()
    {
        // Ensure IsFalling is true only when truly airborne and moving downwards, not due to a jump
        if (!IsGrounded && !IsJumping && _rb.linearVelocity.y < -0.1f) // Check for downward velocity
        {
            IsFalling = true;
            Friction(false); // Remove friction while falling
        }
        // IsFalling will be set to false by HandleGrounded when landing
    }

    public void Friction(bool mode)
    {
        if (_playerCollider == null || _playerCollider.sharedMaterial == null) return;

        if (mode)
        {
            _playerCollider.sharedMaterial.staticFriction = 1; //
            _playerCollider.sharedMaterial.dynamicFriction = 1; //
        }
        else
        {
            _playerCollider.sharedMaterial.staticFriction = 0; //
            _playerCollider.sharedMaterial.dynamicFriction = 0; //
        }
    }

    public Vector3 ComputeAvoidanceVector()
    {
        Vector3 avoidance = Vector3.zero;
        Vector3 leftEyeDirection = Quaternion.Euler(0, -raycastAngle, 0) * transform.forward;
        Vector3 rightEyeDirection = Quaternion.Euler(0, raycastAngle, 0) * transform.forward;

        RaycastHit leftHitInfo;
        RaycastHit rightHitInfo;

        bool leftHit = Physics.Raycast(transform.position + leftEyeOffset, leftEyeDirection, out leftHitInfo, raycastDistance, _tileObstacleMask);
        bool rightHit = Physics.Raycast(transform.position + rightEyeOffset, rightEyeDirection, out rightHitInfo, raycastDistance, _tileObstacleMask);

        if (leftHit && rightHit)
        {
            avoidance = (Vector3.Cross(leftHitInfo.normal, Vector3.up) + Vector3.Cross(rightHitInfo.normal, Vector3.up)).normalized; //
        }
        else if (leftHit)
        {
            avoidance = Vector3.Cross(leftHitInfo.normal, Vector3.up).normalized; //
        }
        else if (rightHit)
        {
            avoidance = Vector3.Cross(rightHitInfo.normal, Vector3.up).normalized; //
        }

        return avoidance;
    }


    // Helpers for Gizmos (Editor only)
    [HideInInspector] public Vector3 WallDetectPosition => (_playerCollider.transform.position - WallDetectionOffset) + _playerCollider.transform.forward * _wallCheckOffset; //
    [HideInInspector] public Vector3 BombDetectPosition => (_playerCollider.transform.position - BombDetectionOffset) + _playerCollider.transform.forward * _bombCheckOffset; //

    // Draw Gizmos for debugging in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black; //
        Gizmos.DrawWireSphere(transform.position + _grounderOffset, _grounderRadius); //
        Gizmos.color = Color.red; //
        Gizmos.DrawWireSphere(WallDetectPosition, _wallCheckRadius); //
        Gizmos.DrawWireSphere(BombDetectPosition, _bombCheckRadius); //

        // Draw raycasts for obstacle avoidance
        Gizmos.color = Color.blue; //
        Vector3 leftEyeDirection = Quaternion.Euler(0, -raycastAngle, 0) * transform.forward; //
        Vector3 rightEyeDirection = Quaternion.Euler(0, raycastAngle, 0) * transform.forward; //
        Gizmos.DrawRay(transform.position + leftEyeOffset, leftEyeDirection * raycastDistance); //
        Gizmos.DrawRay(transform.position + rightEyeOffset, rightEyeDirection * raycastDistance); //

        // Draw slide spheres
        DrawSlideSpheres(); //
    }

    // Using transform.position for these as well
    private Vector3 Front => (transform.position - forwardOffset) + transform.forward * frontoffs; //
    private Vector3 Back => (transform.position - backOffset) + transform.forward * backoffs; //

    // Public method for drawing slide spheres (can be called from OnDrawGizmos)
    public void DrawSlideSpheres()
    {
        Gizmos.color = Color.cyan; //
        Gizmos.DrawWireSphere(Front, slideCheckerRadius); //
        Gizmos.DrawWireSphere(Back, slideCheckerRadius); //
    }

    public void SlideFlags()
    {
        Collider[] slideForwardHits = new Collider[1]; //
        Collider[] slideBackHits = new Collider[1]; //

        bool slideForward = Physics.OverlapSphereNonAlloc(Front, slideCheckerRadius, slideForwardHits, _obstacleMask) > 0; //
        bool slideBack = Physics.OverlapSphereNonAlloc(Back, slideCheckerRadius, slideBackHits, _obstacleMask) > 0; //

        if ((slideForward || slideBack) && !IsGrounded && IsFalling && CurrentMoveDirection == Vector3.zero) //
        {
            Vector3 slideDirection = Vector3.zero; //
            Vector3 forceDirection = Quaternion.Euler(slideAngle, 0f, 0f) * transform.forward; //

            if (slideForward) slideDirection += -transform.forward; //
            if (slideBack) slideDirection += transform.forward; //

            Vector3 totalForce = (slideDirection * slideForce) + (forceDirection * slideForce); //
            _rb.AddForce(totalForce, ForceMode.Impulse); //
        }
    }

    public void UpdateCurrentTile()
    {
        RaycastHit hit; //
        if (Physics.Raycast(transform.position, Vector3.down, out hit, tileRayLength, _tileLayer)) //
        {
            Tile hitTile = hit.collider.GetComponent<Tile>(); //
            if (hitTile != null) //
            {
                CurrentTile = hitTile; //
            }
            else
            {
                CurrentTile = null; //
            }
        }
        else
        {
            CurrentTile = null; //
        }
    }

    public Vector3 FindNeighbouringTilePosition()
    {
        if (CurrentTile == null) //
        {
            Debug.LogWarning("Player is not on a tile, cannot find neighboring tile."); //
            return transform.position + transform.forward; //
        }

        Vector3 facingDirection = transform.forward; //
        RaycastHit hit; //

        if (Physics.Raycast(CurrentTile.transform.position, facingDirection, out hit, raycastDistance, _tileLayer)) //
        {
            Tile nextTile = hit.collider.GetComponent<Tile>(); //
            if (nextTile != null) //
            {
                return nextTile.transform.position; //
            }
            else
            {
                return CurrentTile.transform.position + facingDirection; //
            }
        }
        else
        {
            return CurrentTile.transform.position + facingDirection; //
        }
    }

    public Tile GetNeighbouringTile()
    {
        if (CurrentTile == null) return null; //

        Vector3 facingDirection = transform.forward; //
        RaycastHit hit; //

        if (Physics.Raycast(CurrentTile.transform.position, facingDirection, out hit, raycastDistance, _tileLayer)) //
        {
            return hit.collider.GetComponent<Tile>(); //
        }
        return null; //
    }
}