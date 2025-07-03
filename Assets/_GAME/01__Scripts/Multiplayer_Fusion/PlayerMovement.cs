using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private PlayerController _playerController;
    private PlayerControls _playerControls;
    private Player _player; //
    private Rigidbody _rb; //
    private CapsuleCollider _playerCollider; //
    private RigidbodyConstraints _originalConstraints; // Store original constraints
    private PlayerInputHandler _inputHandler; // Reference to the input handler
    private PlayerAttack _playerAttack; // Reference to the player attack script
    [HideInInspector] public bool justLanded = false; // Indicates player just landed from a jump/fall
    public float landedTime = 0f; // To track when the player landed
    public float LAND_GRACE_PERIOD = 0.15f; // Adjust this value as needed (e.g., 0.1s to 0.2s)
    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 2f; //
    public float _jumpForce = 5.8f; //
    [SerializeField] private ParticleSystem _jumpParticle; // Assign in inspector
    [HideInInspector] public bool justJumpedOutOfPush = false;

    [HideInInspector] public bool hasRecentlyFallen;
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
    public float fallHeight;
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
    private bool canMove = true;

    public bool CanMove
    {
        get => canMove;
        set
        {
            if (canMove != value)
            {
                canMove = value;

                if (!canMove)
                {
                    var stackTrace = new System.Diagnostics.StackTrace(1, true); // Skip the property setter itself
                    UnityEngine.Debug.LogWarning($"CanMove was set to FALSE by:\n{stackTrace}");
                }
            }
        }
    }
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
    [SerializeField] private bool canPush = true;
    [SerializeField] private Vector3 moveDirection;
    public bool CanPush
    {
        get => canPush;
        set
        {
            if (canPush != value)
            {
                canPush = value;

                if (!canPush)
                {
                    var stackTrace = new System.Diagnostics.StackTrace(1, true); // Skip the property setter itself
                    UnityEngine.Debug.LogWarning($"CanPush was set to FALSE by:\n{stackTrace}");
                }
            }
        }
    }


    public void Initialize(Player player, Rigidbody rb, CapsuleCollider collider, PlayerController pController, PlayerControls pControls)
    {
        _player = player;
        _rb = rb;
        _playerCollider = collider;
        _originalConstraints = _rb.constraints;
        _playerController = pController;
        _playerControls = pControls;
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
        CanPush = true;
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
        moveDirection = CurrentMoveDirection;
        if (!CanMove && !_playerController.isPushing)
        {
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            CurrentMoveDirection = Vector3.zero;
            CurrentCalculatedMoveSpeed = 0f;
            // HandleMovement(_inputHandler.MoveInput);
        }
        else
        {
            HandleMovement(_inputHandler.MoveInput);
        }
        HandleGrounded();
        Fall();
        SlideFlags();
        CheckObstaclesBehindAndAhead();

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

        if (jumpInputDetected && IsGrounded && !IsJumping && !IsPulling)
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

        Vector3 desiredMoveDirection = new Vector3(input.x, 0f, input.y);

        if (desiredMoveDirection != Vector3.zero)
        {
            transform.forward = desiredMoveDirection;
            CurrentMoveDirection = desiredMoveDirection;
        }
        else
        {
            CurrentMoveDirection = Vector3.zero;
        }

        Vector3 avoidanceVector = ComputeAvoidanceVector();
        Vector3 finalMovementDirection = CurrentMoveDirection;

        // Apply avoidance if moving and not falling and not pushing
        if (CurrentMoveDirection != Vector3.zero && !IsFalling && !_playerController.isPushing)
        {
            bool leftEyeHitObstacle = Physics.Raycast(transform.position + leftEyeOffset, Quaternion.Euler(0, -raycastAngle, 0) * transform.forward, raycastDistance, _tileObstacleMask);
            bool rightEyeHitObstacle = Physics.Raycast(transform.position + rightEyeOffset, Quaternion.Euler(0, raycastAngle, 0) * transform.forward, raycastDistance, _tileObstacleMask);

            if (leftEyeHitObstacle || rightEyeHitObstacle)
            {
                finalMovementDirection = CurrentMoveDirection + avoidanceVector * avoidanceMultiplier;
            }
        }

        CurrentCalculatedMoveSpeed = (_player != null && CurrentMoveDirection != Vector3.zero) ? _player.MoveSpeed : 0f;

        // Use different movement methods based on whether pushing or not
        if (_playerController.isPushing)
        {
            // Use MovePosition when pushing (same as obstacle)
            Vector3 targetPosition = transform.position + finalMovementDirection * CurrentCalculatedMoveSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(targetPosition);
        }
        else
        {
            // Use velocity for normal movement
            Vector3 targetVelocity = finalMovementDirection * CurrentCalculatedMoveSpeed;
            _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
        }
    }

    public Vector3 GetFacingDirection()
    {
        float currentRotation = transform.eulerAngles.y;
        if (currentRotation == 270)
            return new Vector3(-1, 0, 0);
        if (currentRotation == 90)
            return new Vector3(1, 0, 0);
        if (currentRotation == 0)
            return new Vector3(0, 0, 1);
        if (currentRotation == 180)
            return new Vector3(0, 0, -1);
        else
            return Vector3.zero;
    }
    public bool HasChangedDirection(Vector3 previousDirection)
    {
        return CurrentMoveDirection != previousDirection && CurrentMoveDirection != Vector3.zero;
    }
    public void StopMovement()
    {
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        CurrentMoveDirection = Vector3.zero;
        CurrentCalculatedMoveSpeed = 0f;
    }
    public void SetMovementEnabled(bool enabled)
    {
        CanMove = enabled;
        if (!enabled)
            StopMovement();
    }

    public void Jump()
    {
        if (IsPulling) return;
        IsJumping = true;
        IsGrounded = false;
        if (_jumpParticle != null)
        {
            _jumpParticle.Play();
        }
        if (_playerController.isPushing) // If player was pushing *before* this jump
        {
            Debug.Log("It's currently pushing, attempting to break push on jump.");
            if (_playerController.playerObstacleController.pushObstacle != null)
            {
                _playerController.playerObstacleController.pushObstacle.isBeingPushed = false;
                _playerController.playerObstacleController.pushObstacle = null;
                Debug.Log("Cleaning obstacle before push jump");
            }
            _playerController.isPushing = false;
            _playerController.StopPush();
            justJumpedOutOfPush = true;
        }
        Friction(false);
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, _rb.linearVelocity.z);

        if (_player != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJumpSound(_player.characterStats.female, transform.position);
        }
    }
    void Update()
    {
        // Debug.Log("Linear Velocity: " + _rb.linearVelocity);
    }
    public Collider[] groundHits = new Collider[1];
    [HideInInspector] public Collider[] wallHits = new Collider[1];
    public bool HandleGrounded()
    {
        // OverlapSphereNonAlloc is more performant than OverlapSphere if used in FixedUpdate
        // Use a small array to reduce garbage
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
            IsJumping = false;
            justLanded = true; // Set flag when landing
            landedTime = Time.time; // Record landing time
            justJumpedOutOfPush = false; // Reset if this was a jump out of push
            Friction(true);
        }
        else if (IsGrounded && !newGrounded)
        {
            IsGrounded = false;
            hasRecentlyFallen = false;
            if (!IsJumping && _rb.linearVelocity.y < -0.1f)
            {
                IsFalling = true;
            }
            justLanded = false; // Reset if no longer grounded
        }

        // Update wall/bomb blocked states (these could be networked if relevant for gameplay)

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
        // if (IsAgainstWall && IsJumping)
        if (justLanded && (Time.time - landedTime) > LAND_GRACE_PERIOD)
        {
            justLanded = false;
        }


        //     CanMove = false;
        return newGrounded;
    }

    public void Fall()
    {
        // Ensure IsFalling is true only when truly airborne and moving downwards, not due to a jump
        if (!IsGrounded && !IsJumping && _rb.linearVelocity.y < -0.1f) // Check for downward velocity
        {
            IsFalling = true;
            Friction(false); // Remove friction while falling
            hasRecentlyFallen = true; // Set flag to indicate player has fallen recently
            fallHeight = transform.position.y; // Calculate fall height from grounder offset
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
    [HideInInspector] public Vector3 BombDetectPosition => (_playerCollider.transform.position - BombDetectionOffset) + _playerCollider.transform.forward * _bombCheckOffset;
    public bool pullObstacleFlag, obstacleBehind;
    [SerializeField] LayerMask _behindObstacleMask;
    [SerializeField] public LayerMask _collectibleMask;
    private void CheckObstaclesBehindAndAhead()
    {

        Vector3 obstacleBehindWhilePulling = Quaternion.Euler(0, 0, 0) * (transform.forward * -1);
#if UNITY_EDITOR
        Debug.DrawRay(
            transform.position,
            obstacleBehindWhilePulling * raycastDistance,
            Color.red
        );
#endif
        Ray rayBack = new Ray(transform.position, transform.forward * -1);
        RaycastHit hitBack;
        obstacleBehind = Physics.Raycast(rayBack, out hitBack, 0.6f, _behindObstacleMask);
        // if (obstacleBehind) Debug.Log("Theres an obstacle behind");
        /**
       **Forward Cast
       */
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (_playerController.AI)
            return;
        pullObstacleFlag =
            Physics.Raycast(ray, out hit, 0.5f, _obstacleMask)
            || Physics.Raycast(ray, out hit, 0.5f, _collectibleMask);
        // Debug.Log("pullObstacleFlag : " + pullObstacleFlag);
        _playerControls.Interactable(pullObstacleFlag);
        if (_playerControls == null) Debug.LogError("Playercontrols are null");
        if (_playerControls.levelGoal == null) Debug.LogError("PlayerLevelGoal is null");
        if (_playerControls == null) Debug.LogError("Playercontrols are null");
        if (_playerControls != null &&
            !_playerControls.isPulling
            && _playerControls.levelGoal.levelType == LevelGoal.LevelType.Pull
        )
            _playerControls.hintPull.gameObject.SetActive(pullObstacleFlag);
    }
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


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _originalConstraints = _rb.constraints;
        _playerAttack = GetComponent<PlayerAttack>();
        // playerController = GetComponent<PlayerController>();
    }

    /// <summary>
    /// Applies constraints and disables movement during pull.
    /// </summary>
    public void SetPullConstraints(Obstacle obstacle = null)
    {
        if (obstacle != null && obstacle.grounded)
        {
            IsPulling = true;
            IsPushing = false;
            CanMove = false;
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }
    }

    /// <summary>
    /// Resets constraints and restores movement after pull ends.
    /// </summary>
    public void ResetPullConstraints(Obstacle obstacle = null)
    {
        if (!IsPulling) return;

        if (_player != null)
        {
            _player.MoveSpeed = _player.hasSpeedBuff ? _player.buffedSpeed : _player.StartingMoveSpeed;
        }

        if (!_playerAttack.hittingDown) // Assuming playerAttack is available
            CanMove = true;

        IsPulling = false;
        _rb.isKinematic = false;
        _rb.constraints = _originalConstraints;

        if (obstacle != null)
        {
            obstacle.recentlyPulled = true;
            obstacle.ResetObstacle(); // if method exists
        }
    }

}