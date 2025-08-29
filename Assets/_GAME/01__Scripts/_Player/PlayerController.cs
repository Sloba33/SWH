using UnityEngine;
using Cinemachine;
public class PlayerController : MonoBehaviour
{
    public bool AI;
    [HideInInspector] public PlayerMovement _movement;
    private PlayerInputHandler _input;
    private PlayerAnimation _animation;
    public PlayerObstacleController playerObstacleController;
    private Player _player;
    public PlayerControls _playerControls;
    private CapsuleCollider _playerCollider;
    private Rigidbody _rb;
    public bool pullButtonHeld = false;
    public bool pullButtonReleased = false;
    private Vector3 _previousMoveDirection;
    public bool flashlightEnabled;
    [SerializeField] private GameObject flashlight, spotlight;
    [Header("Camera")]
    public CinemachineFreeLook followCamera;
    public Camera playerCamera;
    public Transform cameraFollowTarget;


    [Header("Pull/Push")]
    public bool isPulling = false;
    public bool isPushing = false;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _input = GetComponent<PlayerInputHandler>();
        _animation = GetComponent<PlayerAnimation>();
        playerObstacleController = GetComponent<PlayerObstacleController>();
        _player = GetComponent<Player>();
        _playerControls = FindFirstObjectByType<PlayerControls>();
        _rb = GetComponent<Rigidbody>();
        _playerCollider = GetComponent<CapsuleCollider>();
        if (_movement != null)
        {
            _movement.Initialize(_player, _rb, _playerCollider, this, _playerControls);
        }
    }

    private void Start()
    {
        if (followCamera != null && cameraFollowTarget != null)
        {
            followCamera.Follow = cameraFollowTarget;
        }
        if (!GameManager.Instance.DarkLevel)
        {
            flashlight.gameObject.SetActive(false);
            spotlight.gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {

        HandleFlashlightDirection();
        HandlePushDirectionChange();

        // Handle input-based triggers for pull/push
        if (_input.GetPullPressedThisFrame())
        {
            StartPull();
        }
        // _movement.IsPushing = isPushing;
    }

    private void HandleFlashlightDirection()
    {
        if (flashlight == null)
            return;
        if (!flashlightEnabled)
        {

            flashlight.gameObject.SetActive(false);
            return;
        }
        else
            flashlight.gameObject.SetActive(true);
        if (_movement.CurrentMoveDirection != Vector3.zero && _movement.CurrentMoveDirection != _previousMoveDirection)
        {
            // Instantiate(flashlight, transform.position, Quaternion.LookRotation(_previousMoveDirection));
            _previousMoveDirection = _movement.CurrentMoveDirection;
        }
    }

    private void HandlePushDirectionChange()
    {
        if (playerObstacleController == null || _movement.CurrentMoveDirection == Vector3.zero)
            return;

        if (_movement.CurrentMoveDirection != _previousMoveDirection)
        {
            playerObstacleController.StopPush();
            playerObstacleController.pushDirectionChanged = true;
            _previousMoveDirection = _movement.CurrentMoveDirection;
        }
    }

    public void StartPull()
    {
        isPulling = true;
        pullButtonHeld = true;
        pullButtonReleased = false;

        _movement.IsPulling = true;
    }

    public void StopPull()
    {
        isPulling = false;
        pullButtonHeld = false;
        pullButtonReleased = true;

        _movement.IsPulling = false;
    }

    public void StartPush()
    {
        isPushing = true;
        _movement.IsPushing = true;
        // Let PlayerAnimation naturally detect push from state
        // _animation.EnterState(PlayerAnimState.Push);
    }

    public void StopPush()
    {
        isPushing = false;
        _movement.IsPushing = false;
        // _animation.EnterState(PlayerAnimState.Idle);
    }
    public Obstacle FindObstacle()
    {
        if (_movement.wallHits[0] != null)
        {
            Debug.Log("[FindObstacle] Found: " + _movement.wallHits[0].gameObject.name);
            return _movement.wallHits[0].gameObject.GetComponent<Obstacle>();
        }

        Debug.Log("[FindObstacle] No obstacle found");
        return null;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            Debug.Log("Power-up collision detected" + other);
            CollectibleItem col = other.GetComponentInParent<CollectibleItem>();
            if (col != null)
            {

                col.Collect(this);
                Debug.Log("Trigger name " + other.name);

            }
        }
        if (other.CompareTag("Trap"))
        {
            _player.Die();
            // StartCoroutine(player.LoseLevel());
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (_movement.allowFallOffEdges && collision.gameObject.layer == LayerMask.NameToLayer("Boundary"))
        {
            // Ignore collision only for player
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
        }
    }

}

// ----------------------------------------------------------------------------------






// Define the networked input struct for Fusion


// public class PlayerController : NetworkBehaviour
// {
//     // --- Player References ---
//     [Title("Player References", TitleColor.Violet, TitleColor.Violet, 2f, 20f)]
//     [SerializeField] private Player player;
//     [SerializeField] private PlayerAttack playerAttack;
//     [HideInInspector] public PlayerObstacleController playerObstacleController;
//     [SerializeField] public PlayerMovement playerMovement; // Reference to PlayerMovement
//     [SerializeField] private PlayerAnimation playerAnimation; // Reference to PlayerAnimation

//     // --- General Unity References ---
//     [SerializeField] private ParticleSystem jumpParticle; // Still used for visual effect directly
//     public GameObject camerasPrefab;
//     public Camera playerRegularCamera;
//     Rigidbody _rb; // Keeping Rigidbody reference as PlayerController might still interact with it (e.g., for kinematic toggling)
//     CapsuleCollider playerCollider; // Keeping Collider reference for general player setup
//     RigidbodyConstraints _originalConstraints; // Keeping for general physics management

//     // --- Flashlight ---
//     public GameObject flashlight, spotlight; // Assign the flashlight prefab in the inspector
//     private Vector3 previousFlashlightDirection; // For flashlight direction

//     // --- UI/Controls ---
//     public bool AI; // For AI player distinction
//     Button _pullButton; // UI button for pulling (likely handled by PlayerControls/PlayerInteraction)
//     [SerializeField] FloatingJoystick floatingJoystick; // Joystick reference
//     [SerializeField] DynamicJoystick dynamicJoystick; // Joystick reference

//     #region Controls & Input
//     [Title("Controls", TitleColor.Blue, TitleColor.Blue, 2f, 20f)]
//     [SerializeField] private Controls controls; // Your existing enum for control schemes
//     [SerializeField] private MovementType movementType; // Your existing enum for movement types

//     // New Input System
//     [SerializeField] private PlayerInput playerInput; // Reference to the PlayerInput component

//     // Local input states gathered in OnInput
//     private Vector2 _localMoveInputVector;
//     private bool _localJumpButtonPressed;
//     private bool _localAttackHitPressed;
//     private bool _localAttackHitDownPressed;

//     // UI/Game Manager related references
//     [HideInInspector] public PlayerControls playerControls;
//     public GameObject playerCamera;

//     #endregion

//     // --- Interaction States ---
//     [Title("Interaction", TitleColor.Indigo, TitleColor.Indigo, 2f, 20f)]
//     [NaughtyAttributes.ReadOnly][SerializeField] public bool isPushing;
//     [SerializeField] public bool canPush = true;
//     [NaughtyAttributes.ReadOnly][SerializeField] public bool isPulling;
//     public bool recentlyHitWall;
//     public bool obstacleBehind;
//     public bool _pullButtonHeld;
//     public bool _pullButtonReleased;

//     // --- Level Specific Data ---
//     private List<TileCollision> tileColliders = new List<TileCollision>();
//     Tile tile; // Player's current tile reference
//     public Obstacle obstacle; // Current obstacle reference

//     // --- Collectibles ---
//     [SerializeField] public LayerMask collectibleMask; // Keep if PlayerController interacts with collectibles directly

//     // --- Start / Initialization ---
//     private void Start()
//     {
//         // Initial setup for editor vs device controls
// #if UNITY_EDITOR
//         // controls = Controls.Keyboard; // Uncomment to force keyboard in editor
// #else
//         controls = Controls.Joystick_NewInputSystem;
// #endif

//         // Audio Listener Debug
//         List<AudioListener> audioListeners = new List<AudioListener>(FindObjectsOfType<AudioListener>());
//         foreach (var listener in audioListeners)
//         {
//             Debug.Log("Found AudioListener on: " + listener.gameObject.name);
//         }

//         // Get Component References
//         player = GetComponent<Player>();
//         _rb = GetComponent<Rigidbody>();
//         playerCollider = GetComponent<CapsuleCollider>();
//         _originalConstraints = _rb.constraints; // Store original constraints
//         playerObstacleController = GetComponent<PlayerObstacleController>();
//         playerAttack = GetComponent<PlayerAttack>();
//         playerMovement = GetComponent<PlayerMovement>(); // Get PlayerMovement component
//         playerAnimation = GetComponent<PlayerAnimation>(); // Get PlayerAnimation component
//         playerInput = GetComponent<PlayerInput>(); // Get PlayerInput component

//         // Initialize PlayerMovement with core components
//         if (playerMovement != null)
//         {
//             playerMovement.Initialize(player, _rb, playerCollider);
//         }
//         else
//         {
//             Debug.LogError("PlayerController: PlayerMovement component not found!");
//         }
//         // PlayerAnimation is initialized in its own Spawned method

//         // UI & Game Manager References
//         if (!AI)
//         {
//             playerControls = FindFirstObjectByType<PlayerControls>();
//             if (playerControls != null)
//             {
//                 playerControls.playerController = this;
//                 playerControls.playerCamera = camerasPrefab;
//             }
//             Settings settings = FindFirstObjectByType<Settings>();
//             if (settings != null)
//             {
//                 settings.playerController = this;
//                 settings.cameraController = camerasPrefab.GetComponent<CameraController>();
//             }
//             _pullButton = playerControls?.pullButton; // Null-check for safety
//         }

//         // Find existing tile colliders
//         TileCollision[] foundColliders = FindObjectsOfType<TileCollision>();
//         tileColliders.AddRange(foundColliders);

//         // Initial Player Position
//         if (!AI && GameManager.Instance != null && GameManager.Instance.playerSpawnPoint != null)
//             transform.position = GameManager.Instance.playerSpawnPoint.position;
//         if (AI) playerControls = null;

//         // Flashlight setup
//         if (GameManager.Instance != null && !GameManager.Instance.DarkLevel)
//         {
//             spotlight.gameObject.SetActive(false);
//             flashlight.gameObject.SetActive(false);
//         }
//         previousFlashlightDirection = transform.forward;
//     }

//     // --- INetworkRunnerCallbacks Implementation ---

//     // This method is called by the NetworkRunner when it needs input for a new tick.
//     public void OnInput(NetworkRunner runner, NetworkInput input)
//     {
//         // Only the client with Input Authority should provide input
//         if (Object.HasInputAuthority)
//         {
//             // Reset input states for the current frame
//             _localMoveInputVector = Vector2.zero;
//             _localJumpButtonPressed = false;
//             _localAttackHitPressed = false;
//             _localAttackHitDownPressed = false;

//             // --- Gather continuous movement input ---
//             if (!isPulling && playerMovement._canMove) // Only gather movement if not pulling and can move
//             {
//                 if (controls == Controls.Joystick)
//                 {
//                     _localMoveInputVector.x = dynamicJoystick != null ? dynamicJoystick.Horizontal : 0f;
//                     _localMoveInputVector.y = dynamicJoystick != null ? dynamicJoystick.Vertical : 0f;
//                 }
//                 else if (controls == Controls.Keyboard)
//                 {
//                     _localMoveInputVector.x = Input.GetAxisRaw("Horizontal");
//                     _localMoveInputVector.y = Input.GetAxisRaw("Vertical");
//                 }
//                 else if (controls == Controls.Joystick_NewInputSystem && playerInput != null)
//                 {
//                     _localMoveInputVector = playerInput.actions["Move"].ReadValue<Vector2>();
//                 }
//             }

//             // --- Gather discrete button inputs (WasPressedThisFrame ensures one-shot per client frame) ---
//             if (playerInput != null)
//             {
//                 if (playerInput.actions["Jump"].WasPressedThisFrame())
//                 {
//                     _localJumpButtonPressed = true;
//                 }
//                 if (playerInput.actions["Hit"].WasPressedThisFrame()) // Assuming "Hit" action for C key
//                 {
//                     _localAttackHitPressed = true;
//                 }
//                 if (playerInput.actions["HitDown"].WasPressedThisFrame()) // Assuming "HitDown" action for X key
//                 {
//                     _localAttackHitDownPressed = true;
//                 }
//                 // Add other button presses here
//             }

//             // Construct the NetworkInputData
//             NetworkInputData data = new NetworkInputData();
//             data.MoveDirection = new Vector3(_localMoveInputVector.x, 0, _localMoveInputVector.y);
//             data.JumpPressed = _localJumpButtonPressed;
//             data.HitPressed = _localAttackHitPressed;
//             data.HitDownPressed = _localAttackHitDownPressed;
//             // Set other inputs

//             input.Set(data); // Set the input for the current tick
//         }
//     }

//     // --- FixedUpdateNetwork: Process Networked Input (runs deterministically) ---
//     public override void FixedUpdateNetwork()
//     {
//         // Get the networked input for the current tick
//         if (GetInput(out NetworkInputData input))
//         {
//             Vector3 networkedMoveDirection = input.MoveDirection;
//             bool networkedJumpInput = input.JumpPressed;
//             bool networkedAttackHit = input.HitPressed;
//             bool networkedAttackHitDown = input.HitDownPressed;

//             // Apply TwoAxis movement restriction to the networked move direction if enabled
//             if (movementType == MovementType.TwoAxis)
//             {
//                 if (Mathf.Abs(networkedMoveDirection.x) > Mathf.Abs(networkedMoveDirection.z))
//                 {
//                     networkedMoveDirection.z = 0;
//                 }
//                 else
//                 {
//                     networkedMoveDirection.x = 0;
//                 }
//             }

//             // Delegate movement to PlayerMovement
//             if (playerMovement != null)
//             {
//                 // Only allow movement if not pulling and PlayerMovement indicates it's allowed
//                 if (!isPulling && playerMovement._canMove)
//                 {
//                     playerMovement.HandleMovement(networkedMoveDirection, networkedJumpInput);
//                 }
//                 else // If player is pulling or unable to move, send zero movement
//                 {
//                     playerMovement.HandleMovement(Vector3.zero, false);
//                 }
//             }
//             else
//             {
//                 Debug.LogError("PlayerController: playerMovement is null in FixedUpdateNetwork!");
//             }

//             // Delegate attacks to PlayerAttack
//             if (playerAttack != null)
//             {
//                 if (networkedAttackHit)
//                 {
//                     playerAttack.Hit();
//                 }
//                 if (networkedAttackHitDown)
//                 {
//                     playerAttack.HitDown();
//                 }
//             }

//             // Flashlight direction update (Base on the networked _currentMoveDirection from PlayerMovement)
//             if (playerMovement != null)
//             {
//                 if (playerMovement.CurrentMoveDirection != Vector3.zero)
//                 {
//                     previousFlashlightDirection = playerMovement.CurrentMoveDirection.normalized;
//                 }

//                 // Audio footsteps (Base on actual movement state from PlayerMovement)
//                 if (!AI)
//                 {
//                     if (playerMovement.CurrentMoveDirection != Vector3.zero && playerMovement.IsGrounded)
//                     {
//                         AudioManager.Instance?.StartPlayerFootsteps(transform.position);
//                     }
//                     else
//                     {
//                         AudioManager.Instance?.StopPlayerFootsteps();
//                     }
//                 }

//                 // Flashlight Instantiate (Base on actual movement state from PlayerMovement)
//                 if (playerMovement.CurrentMoveDirection != previousFlashlightDirection && playerMovement.CurrentMoveDirection != Vector3.zero && GameManager.Instance != null && GameManager.Instance.DarkLevel && flashlight != null && !AI)
//                     InstantiateFlashlight(previousFlashlightDirection);
//             }

//             // Obstacle push/pull logic (rely on networked states/input)
//             if (playerObstacleController != null)
//             {
//                 // This logic needs careful consideration. If playerMovement._currentMoveDirection
//                 // is always updated by HandleMovement, then comparing it to the new networkedMoveDirection
//                 // might be redundant or incorrectly signify a 'change'.
//                 // Ideally, playerObstacleController should have its own methods that take
//                 // player's current direction, or rely on a state change from PlayerMovement.
//                 // For now, retaining the original logic but be mindful of its networked implications.
//                 if (networkedMoveDirection != playerMovement.CurrentMoveDirection) // This comparison is tricky as playerMovement._currentMoveDirection might be updated by now
//                 {
//                     playerObstacleController.StopPush(); // Assuming StopPush resets pushDirectionChanged
//                     if (networkedMoveDirection != Vector3.zero)
//                     {
//                         Debug.Log("Code 0");
//                         Debug.Log("Setting push direction to true");
//                         playerObstacleController.pushDirectionChanged = true;
//                     }
//                 }
//                 // No need for playerMovement.previousMoveDirection = playerMovement.currentMoveDirection;
//                 // as playerMovement now manages its own internal state and _currentMoveDirection is networked.
//             }
//         }
//         else
//         {
//             // If no input is received (e.g., if this is an AI player or a remote client without input authority)
//             // Still ensure movement is handled (e.g., stopping if no input)
//             if (playerMovement != null)
//             {
//                 // If it's a human player but no input (e.g., input dropped), stop movement
//                 if (!AI)
//                 {
//                     playerMovement.HandleMovement(Vector3.zero, false);
//                 }
//                 // For AI, their movement logic would be separate and not driven by networked input here.
//                 // For remote clients, their playerMovement will be updated by the State Authority.
//             }
//         }
//     }

//     // --- Collision/Trigger ---
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Collectible"))
//         {
//             Debug.Log("Power-up collision detected" + other);
//             CollectibleItem col = other.GetComponentInParent<CollectibleItem>();
//             if (col != null)
//             {
//                 col.Collect(this);
//                 Debug.Log("Trigger name " + other.name);
//             }
//         }
//         if (other.CompareTag("Trap"))
//         {
//             player.Die();
//             // StartCoroutine(player.LoseLevel());
//         }
//     }

//     // Flashlight methods
//     private void InstantiateFlashlight(Vector3 direction)
//     {
//         GameObject flashlightObj = Instantiate(flashlight, transform.position, Quaternion.LookRotation(direction));
//         StartCoroutine(ReduceLightCoroutine(flashlightObj.GetComponent<Light>()));
//     }

//     private IEnumerator ReduceLightCoroutine(Light flashlight)
//     {
//         float intensity = flashlight.intensity;
//         while (intensity > 0)
//         {
//             intensity -= 0.2f;
//             flashlight.intensity = intensity;
//             yield return new WaitForSeconds(0.01f);
//         }
//         Destroy(flashlight.gameObject);
//     }

//     // --- Interaction Methods ---
//     public void StartPull()
//     {
//         _pullButtonHeld = true;
//         _pullButtonReleased = false;
//     }

//     public Tile FindCurrentTile()
//     {
//         // Shoot a ray down from the player's position, filtering by the "Tile" layer
//         RaycastHit hit;
//         if (Physics.Raycast(transform.position, Vector3.down, out hit, playerMovement.tileRayLength, playerMovement._tileLayer))
//         {
//             // Check if the hit object has a Tile component
//             Tile hitTile = hit.collider.GetComponent<Tile>();
//             if (hitTile != null)
//             {
//                 // Found the tile below the player
//                 tile = hitTile;
//                 return tile;
//             }
//             else
//                 return null;
//         }
//         else
//             return null;
//     }

//     public void StopPull()
//     {
//         isPulling = false;
//         _pullButtonHeld = false;
//         _pullButtonReleased = true;
//     }

//     public void SetPullConstraints(Obstacle obstacle)
//     {
//         if (_pullButtonHeld)
//         {
//             isPulling = true;
//             isPushing = false;
//             if (obstacle != null && obstacle.grounded)
//                 if (obstacle != null)
//                 {
//                     playerMovement._canMove = false; // Control movement from here
//                     _rb.isKinematic = true;
//                     _rb.constraints =
//                         RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
//                 }
//                 else
//                 {
//                     _rb.constraints = _originalConstraints;
//                 }
//         }
//     }

//     public void ResetPullConstraints(Obstacle obstacle)
//     {
//         if (!_pullButtonHeld && _pullButtonReleased)
//         {
//             if (!player.hasSpeedBuff)
//                 player.MoveSpeed = player.StartingMoveSpeed;
//             else
//                 player.MoveSpeed = player.buffedSpeed;
//             if (!playerAttack.hittingDown)
//                 playerMovement._canMove = true; // Control movement from here
//             _pullButtonReleased = false;
//             _rb.isKinematic = false;
//             _rb.constraints = _originalConstraints;
//             isPulling = false;
//             if (obstacle != null)
//             {
//                 obstacle.recentlyPulled = true;
//                 ResetObstacle(obstacle, "Key was let go");
//             }
//         }
//     }

//     public void ResetObstacle(Obstacle obstacle, string str)
//     {
//         isPushing = false;
//         isPulling = false;
//         if (obstacle != null)
//         {
//             obstacle.isBeingPushed = false;
//             obstacle.isBeingPulled = false;
//             obstacle.isPositioned = false;
//             obstacle.isHeightPositioned = false;
//             obstacle.isPullable = true;
//             obstacle = null; // Clear reference
//         }
//         // if (obstacleBackwards != null) // This variable is not declared in PlayerController,
//         // obstacleBackwards = null;    // assuming it's part of PlayerObstacleController or a typo.
//         // Keep commented as it was.
//     }
//     public Vector3 FindNeighbouringTile()
//     {
//         // Assuming the player's facing direction is forward (you might need to adjust this based on your setup)
//         Vector3 facingDirection = transform.forward;
//         tile = FindCurrentTile();
//         // Cast a ray in the facing direction from the current tile position
//         RaycastHit hit;
//         if (Physics.Raycast(tile.transform.position, facingDirection, out hit, 0.99f))
//         {
//             // Check if the hit object has a Tile component
//             Tile nextTile = hit.collider.GetComponent<Tile>();
//             if (nextTile != null)
//             {
//                 // Found the next tile in the facing direction
//                 return nextTile.transform.position;
//             }
//             else
//                 return tile.transform.position + facingDirection;
//         }
//         else
//             return tile.transform.position + facingDirection;
//     }

// }

// // Enums - ensure these are accessible (e.g., in their own file or a common namespace)
// public enum Controls
// {
//     Keyboard,
//     Joystick,
//     Joystick_NewInputSystem
// }

// public enum MovementType
// {
//     EightAxis,
//     TwoAxis
// }