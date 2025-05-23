using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TetraCreations.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Fusion;
using Fusion.Sockets; // Required for INetworkRunnerCallbacks

// Define the networked input struct for Fusion


public class PlayerController : NetworkBehaviour
{
    // --- Player References ---
    [Title("Player References", TitleColor.Violet, TitleColor.Violet, 2f, 20f)]
    [SerializeField] private Player player;
    [SerializeField] private PlayerAttack playerAttack;
    [HideInInspector] public PlayerObstacleController playerObstacleController;
    [SerializeField] public PlayerMovement playerMovement; // Reference to PlayerMovement
    [SerializeField] private PlayerAnimation playerAnimation; // Reference to PlayerAnimation

    // --- General Unity References ---
    [SerializeField] private ParticleSystem jumpParticle; // Still used for visual effect directly
    public GameObject camerasPrefab;
    public Camera playerRegularCamera;
    Rigidbody _rb; // Keeping Rigidbody reference as PlayerController might still interact with it (e.g., for kinematic toggling)
    CapsuleCollider playerCollider; // Keeping Collider reference for general player setup
    RigidbodyConstraints _originalConstraints; // Keeping for general physics management

    // --- Flashlight ---
    public GameObject flashlight, spotlight; // Assign the flashlight prefab in the inspector
    private Vector3 previousFlashlightDirection; // For flashlight direction

    // --- UI/Controls ---
    public bool AI; // For AI player distinction
    Button _pullButton; // UI button for pulling (likely handled by PlayerControls/PlayerInteraction)
    [SerializeField] FloatingJoystick floatingJoystick; // Joystick reference
    [SerializeField] DynamicJoystick dynamicJoystick; // Joystick reference

    #region Controls & Input
    [Title("Controls", TitleColor.Blue, TitleColor.Blue, 2f, 20f)]
    [SerializeField] private Controls controls; // Your existing enum for control schemes
    [SerializeField] private MovementType movementType; // Your existing enum for movement types

    // New Input System
    [SerializeField] private PlayerInput playerInput; // Reference to the PlayerInput component

    // Local input states gathered in OnInput
    private Vector2 _localMoveInputVector;
    private bool _localJumpButtonPressed;
    private bool _localAttackHitPressed;
    private bool _localAttackHitDownPressed;

    // UI/Game Manager related references
    [HideInInspector] public PlayerControls playerControls;
    public GameObject playerCamera;

    #endregion

    // --- Interaction States ---
    [Title("Interaction", TitleColor.Indigo, TitleColor.Indigo, 2f, 20f)]
    [NaughtyAttributes.ReadOnly][SerializeField] public bool isPushing;
    [SerializeField] public bool canPush = true;
    [NaughtyAttributes.ReadOnly][SerializeField] public bool isPulling;
    public bool recentlyHitWall;
    public bool obstacleBehind;
    public bool _pullButtonHeld;
    public bool _pullButtonReleased;

    // --- Level Specific Data ---
    private List<TileCollision> tileColliders = new List<TileCollision>();
    Tile tile; // Player's current tile reference
    public Obstacle obstacle; // Current obstacle reference

    // --- Collectibles ---
    [SerializeField] public LayerMask collectibleMask; // Keep if PlayerController interacts with collectibles directly

    // --- Start / Initialization ---
    private void Start()
    {
        // Initial setup for editor vs device controls
#if UNITY_EDITOR
        // controls = Controls.Keyboard; // Uncomment to force keyboard in editor
#else
        controls = Controls.Joystick_NewInputSystem;
#endif

        // Audio Listener Debug
        List<AudioListener> audioListeners = new List<AudioListener>(FindObjectsOfType<AudioListener>());
        foreach (var listener in audioListeners)
        {
            Debug.Log("Found AudioListener on: " + listener.gameObject.name);
        }

        // Get Component References
        player = GetComponent<Player>();
        _rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        _originalConstraints = _rb.constraints; // Store original constraints
        playerObstacleController = GetComponent<PlayerObstacleController>();
        playerAttack = GetComponent<PlayerAttack>();
        playerMovement = GetComponent<PlayerMovement>(); // Get PlayerMovement component
        playerAnimation = GetComponent<PlayerAnimation>(); // Get PlayerAnimation component
        playerInput = GetComponent<PlayerInput>(); // Get PlayerInput component

        // Initialize PlayerMovement with core components
        if (playerMovement != null)
        {
            playerMovement.Initialize(player, _rb, playerCollider);
        }
        else
        {
            Debug.LogError("PlayerController: PlayerMovement component not found!");
        }
        // PlayerAnimation is initialized in its own Spawned method

        // UI & Game Manager References
        if (!AI)
        {
            playerControls = FindObjectOfType<PlayerControls>();
            if (playerControls != null)
            {
                playerControls.playerController = this;
                playerControls.playerCamera = camerasPrefab;
            }
            Settings settings = FindObjectOfType<Settings>();
            if (settings != null)
            {
                settings.playerController = this;
                settings.cameraController = camerasPrefab.GetComponent<CameraController>();
            }
            _pullButton = playerControls?.pullButton; // Null-check for safety
        }

        // Find existing tile colliders
        TileCollision[] foundColliders = FindObjectsOfType<TileCollision>();
        tileColliders.AddRange(foundColliders);

        // Initial Player Position
        if (!AI && GameManager.Instance != null && GameManager.Instance.playerSpawnPoint != null)
            transform.position = GameManager.Instance.playerSpawnPoint.position;
        if (AI) playerControls = null;

        // Flashlight setup
        if (GameManager.Instance != null && !GameManager.Instance.DarkLevel)
        {
            spotlight.gameObject.SetActive(false);
            flashlight.gameObject.SetActive(false);
        }
        previousFlashlightDirection = transform.forward;
    }

    // --- INetworkRunnerCallbacks Implementation ---

    // This method is called by the NetworkRunner when it needs input for a new tick.
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Only the client with Input Authority should provide input
        if (Object.HasInputAuthority)
        {
            // Reset input states for the current frame
            _localMoveInputVector = Vector2.zero;
            _localJumpButtonPressed = false;
            _localAttackHitPressed = false;
            _localAttackHitDownPressed = false;

            // --- Gather continuous movement input ---
            if (!isPulling && playerMovement._canMove) // Only gather movement if not pulling and can move
            {
                if (controls == Controls.Joystick)
                {
                    _localMoveInputVector.x = dynamicJoystick != null ? dynamicJoystick.Horizontal : 0f;
                    _localMoveInputVector.y = dynamicJoystick != null ? dynamicJoystick.Vertical : 0f;
                }
                else if (controls == Controls.Keyboard)
                {
                    _localMoveInputVector.x = Input.GetAxisRaw("Horizontal");
                    _localMoveInputVector.y = Input.GetAxisRaw("Vertical");
                }
                else if (controls == Controls.Joystick_NewInputSystem && playerInput != null)
                {
                    _localMoveInputVector = playerInput.actions["Move"].ReadValue<Vector2>();
                }
            }

            // --- Gather discrete button inputs (WasPressedThisFrame ensures one-shot per client frame) ---
            if (playerInput != null)
            {
                if (playerInput.actions["Jump"].WasPressedThisFrame())
                {
                    _localJumpButtonPressed = true;
                }
                if (playerInput.actions["Hit"].WasPressedThisFrame()) // Assuming "Hit" action for C key
                {
                    _localAttackHitPressed = true;
                }
                if (playerInput.actions["HitDown"].WasPressedThisFrame()) // Assuming "HitDown" action for X key
                {
                    _localAttackHitDownPressed = true;
                }
                // Add other button presses here
            }

            // Construct the NetworkInputData
            NetworkInputData data = new NetworkInputData();
            data.MoveDirection = new Vector3(_localMoveInputVector.x, 0, _localMoveInputVector.y);
            data.JumpPressed = _localJumpButtonPressed;
            data.HitPressed = _localAttackHitPressed;
            data.HitDownPressed = _localAttackHitDownPressed;
            // Set other inputs

            input.Set(data); // Set the input for the current tick
        }
    }

    // --- FixedUpdateNetwork: Process Networked Input (runs deterministically) ---
    public override void FixedUpdateNetwork()
    {
        // Get the networked input for the current tick
        if (GetInput(out NetworkInputData input))
        {
            Vector3 networkedMoveDirection = input.MoveDirection;
            bool networkedJumpInput = input.JumpPressed;
            bool networkedAttackHit = input.HitPressed;
            bool networkedAttackHitDown = input.HitDownPressed;

            // Apply TwoAxis movement restriction to the networked move direction if enabled
            if (movementType == MovementType.TwoAxis)
            {
                if (Mathf.Abs(networkedMoveDirection.x) > Mathf.Abs(networkedMoveDirection.z))
                {
                    networkedMoveDirection.z = 0;
                }
                else
                {
                    networkedMoveDirection.x = 0;
                }
            }

            // Delegate movement to PlayerMovement
            if (playerMovement != null)
            {
                // Only allow movement if not pulling and PlayerMovement indicates it's allowed
                if (!isPulling && playerMovement._canMove)
                {
                    playerMovement.HandleMovement(networkedMoveDirection, networkedJumpInput);
                }
                else // If player is pulling or unable to move, send zero movement
                {
                    playerMovement.HandleMovement(Vector3.zero, false);
                }
            }
            else
            {
                Debug.LogError("PlayerController: playerMovement is null in FixedUpdateNetwork!");
            }

            // Delegate attacks to PlayerAttack
            if (playerAttack != null)
            {
                if (networkedAttackHit)
                {
                    playerAttack.Hit();
                }
                if (networkedAttackHitDown)
                {
                    playerAttack.HitDown();
                }
            }

            // Flashlight direction update (Base on the networked _currentMoveDirection from PlayerMovement)
            if (playerMovement != null)
            {
                if (playerMovement.CurrentMoveDirection != Vector3.zero)
                {
                    previousFlashlightDirection = playerMovement.CurrentMoveDirection.normalized;
                }

                // Audio footsteps (Base on actual movement state from PlayerMovement)
                if (!AI)
                {
                    if (playerMovement.CurrentMoveDirection != Vector3.zero && playerMovement.IsGrounded)
                    {
                        AudioManager.Instance?.StartPlayerFootsteps(transform.position);
                    }
                    else
                    {
                        AudioManager.Instance?.StopPlayerFootsteps();
                    }
                }

                // Flashlight Instantiate (Base on actual movement state from PlayerMovement)
                if (playerMovement.CurrentMoveDirection != previousFlashlightDirection && playerMovement.CurrentMoveDirection != Vector3.zero && GameManager.Instance != null && GameManager.Instance.DarkLevel && flashlight != null && !AI)
                    InstantiateFlashlight(previousFlashlightDirection);
            }

            // Obstacle push/pull logic (rely on networked states/input)
            if (playerObstacleController != null)
            {
                // This logic needs careful consideration. If playerMovement._currentMoveDirection
                // is always updated by HandleMovement, then comparing it to the new networkedMoveDirection
                // might be redundant or incorrectly signify a 'change'.
                // Ideally, playerObstacleController should have its own methods that take
                // player's current direction, or rely on a state change from PlayerMovement.
                // For now, retaining the original logic but be mindful of its networked implications.
                if (networkedMoveDirection != playerMovement.CurrentMoveDirection) // This comparison is tricky as playerMovement._currentMoveDirection might be updated by now
                {
                    playerObstacleController.StopPush(); // Assuming StopPush resets pushDirectionChanged
                    if (networkedMoveDirection != Vector3.zero)
                    {
                        Debug.Log("Code 0");
                        Debug.Log("Setting push direction to true");
                        playerObstacleController.pushDirectionChanged = true;
                    }
                }
                // No need for playerMovement.previousMoveDirection = playerMovement.currentMoveDirection;
                // as playerMovement now manages its own internal state and _currentMoveDirection is networked.
            }
        }
        else
        {
            // If no input is received (e.g., if this is an AI player or a remote client without input authority)
            // Still ensure movement is handled (e.g., stopping if no input)
            if (playerMovement != null)
            {
                // If it's a human player but no input (e.g., input dropped), stop movement
                if (!AI)
                {
                    playerMovement.HandleMovement(Vector3.zero, false);
                }
                // For AI, their movement logic would be separate and not driven by networked input here.
                // For remote clients, their playerMovement will be updated by the State Authority.
            }
        }
    }

    // --- Collision/Trigger ---
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
            player.Die();
            // StartCoroutine(player.LoseLevel());
        }
    }

    // Flashlight methods
    private void InstantiateFlashlight(Vector3 direction)
    {
        GameObject flashlightObj = Instantiate(flashlight, transform.position, Quaternion.LookRotation(direction));
        StartCoroutine(ReduceLightCoroutine(flashlightObj.GetComponent<Light>()));
    }

    private IEnumerator ReduceLightCoroutine(Light flashlight)
    {
        float intensity = flashlight.intensity;
        while (intensity > 0)
        {
            intensity -= 0.2f;
            flashlight.intensity = intensity;
            yield return new WaitForSeconds(0.01f);
        }
        Destroy(flashlight.gameObject);
    }

    // --- Interaction Methods ---
    public void StartPull()
    {
        _pullButtonHeld = true;
        _pullButtonReleased = false;
    }

    public Tile FindCurrentTile()
    {
        // Shoot a ray down from the player's position, filtering by the "Tile" layer
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, playerMovement.tileRayLength, playerMovement._tileLayer))
        {
            // Check if the hit object has a Tile component
            Tile hitTile = hit.collider.GetComponent<Tile>();
            if (hitTile != null)
            {
                // Found the tile below the player
                tile = hitTile;
                return tile;
            }
            else
                return null;
        }
        else
            return null;
    }

    public void StopPull()
    {
        isPulling = false;
        _pullButtonHeld = false;
        _pullButtonReleased = true;
    }

    public void SetPullConstraints(Obstacle obstacle)
    {
        if (_pullButtonHeld)
        {
            isPulling = true;
            isPushing = false;
            if (obstacle != null && obstacle.grounded)
                if (obstacle != null)
                {
                    playerMovement._canMove = false; // Control movement from here
                    _rb.isKinematic = true;
                    _rb.constraints =
                        RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                }
                else
                {
                    _rb.constraints = _originalConstraints;
                }
        }
    }

    public void ResetPullConstraints(Obstacle obstacle)
    {
        if (!_pullButtonHeld && _pullButtonReleased)
        {
            if (!player.hasSpeedBuff)
                player.MoveSpeed = player.StartingMoveSpeed;
            else
                player.MoveSpeed = player.buffedSpeed;
            if (!playerAttack.hittingDown)
                playerMovement._canMove = true; // Control movement from here
            _pullButtonReleased = false;
            _rb.isKinematic = false;
            _rb.constraints = _originalConstraints;
            isPulling = false;
            if (obstacle != null)
            {
                obstacle.recentlyPulled = true;
                ResetObstacle(obstacle, "Key was let go");
            }
        }
    }

    public void ResetObstacle(Obstacle obstacle, string str)
    {
        isPushing = false;
        isPulling = false;
        if (obstacle != null)
        {
            obstacle.isBeingPushed = false;
            obstacle.isBeingPulled = false;
            obstacle.isPositioned = false;
            obstacle.isHeightPositioned = false;
            obstacle.isPullable = true;
            obstacle = null; // Clear reference
        }
        // if (obstacleBackwards != null) // This variable is not declared in PlayerController,
        // obstacleBackwards = null;    // assuming it's part of PlayerObstacleController or a typo.
        // Keep commented as it was.
    }
    public Vector3 FindNeighbouringTile()
    {
        // Assuming the player's facing direction is forward (you might need to adjust this based on your setup)
        Vector3 facingDirection = transform.forward;
        tile = FindCurrentTile();
        // Cast a ray in the facing direction from the current tile position
        RaycastHit hit;
        if (Physics.Raycast(tile.transform.position, facingDirection, out hit, 0.99f))
        {
            // Check if the hit object has a Tile component
            Tile nextTile = hit.collider.GetComponent<Tile>();
            if (nextTile != null)
            {
                // Found the next tile in the facing direction
                return nextTile.transform.position;
            }
            else
                return tile.transform.position + facingDirection;
        }
        else
            return tile.transform.position + facingDirection;
    }
 
}

// Enums - ensure these are accessible (e.g., in their own file or a common namespace)
public enum Controls
{
    Keyboard,
    Joystick,
    Joystick_NewInputSystem
}

public enum MovementType
{
    EightAxis,
    TwoAxis
}

// public class PlayerController : NetworkBehaviour
// {

//     private PlayerInput playerInputMP;
//     public bool AI;
//     [Title("Player References", TitleColor.Violet, TitleColor.Violet, 2f, 20f)]
//     [SerializeField] private Player player;
//     [SerializeField] private PlayerAttack playerAttack;
//     [HideInInspector] public PlayerObstacleController playerObstacleController;
//     [SerializeField] PlayerInput playerInput;
//     Rigidbody _rb;
//     private Animator _anim;
//     CapsuleCollider playerCollider;
//     [SerializeField] private ParticleSystem jumpParticle;

//     public GameObject camerasPrefab;
//     public Camera playerRegularCamera;
//     RigidbodyConstraints _originalConstraints;
//     Button _pullButton;
//     private Vector3 previousFlashlightDirection;

//     public GameObject flashlight, spotlight;  // Assign the flashlight prefab in the inspector
//     private void Start()
//     {

// #if UNITY_EDITOR
//         // controls = Controls.Keyboard;
// #else
//         controls = Controls.Joystick_NewInputSystem;
// #endif
//         List<AudioListener> audioListeners = new List<AudioListener>(FindObjectsOfType<AudioListener>());

//         foreach (var listener in audioListeners)
//         {
//             Debug.Log("Found AudioListener on: " + listener.gameObject.name);
//         }

//         player = GetComponent<Player>();
//         _rb = GetComponent<Rigidbody>();
//         _anim = GetComponent<Animator>();
//         _originalConstraints = _rb.constraints;
//         playerCollider = GetComponent<CapsuleCollider>();
//         playerObstacleController = GetComponent<PlayerObstacleController>();
//         playerCollider = GetComponent<CapsuleCollider>();
//         if (!AI)
//         {

//             playerControls = FindObjectOfType<PlayerControls>();
//             playerControls.playerController = this;
//             playerControls.playerCamera = camerasPrefab;
//             Settings settings = FindObjectOfType<Settings>();
//             settings.playerController = this;
//             settings.cameraController = camerasPrefab.GetComponent<CameraController>();
//             // controls.SetReferences();



//             playerInput = GetComponent<PlayerInput>();

//             _pullButton = playerControls.pullButton;
//         }
//         playerAttack = GetComponent<PlayerAttack>();

//         TileCollision[] foundColliders = FindObjectsOfType<TileCollision>();
//         tileColliders.AddRange(foundColliders);
//         if (!AI) transform.position = GameManager.Instance.playerSpawnPoint.position;
//         if (AI) playerControls = null;
//         if (!GameManager.Instance.DarkLevel)
//         {
//             spotlight.gameObject.SetActive(false);
//             flashlight.gameObject.SetActive(false);
//         }
//         previousFlashlightDirection = transform.forward;
//         if (!AI) female = player.characterStats.female;
//     }

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

//     public void StartPull()
//     {
//         _pullButtonHeld = true;
//         _pullButtonReleased = false;
//     }

//     public void StopPull()
//     {
//         isPulling = false;

//         _pullButtonHeld = false;
//         _pullButtonReleased = true;
//     }
//     [SerializeField]
//     FloatingJoystick floatingJoystick;
//     [SerializeField]
//     DynamicJoystick dynamicJoystick;

//     #region Controls
//     [Title("Controls", TitleColor.Blue, TitleColor.Blue, 2f, 20f)]
//     [SerializeField]
//     Controls controls;

//     [SerializeField]
//     MovementType movementType;
//     private FrameInputs _inputs;
//     private List<TileCollision> tileColliders = new List<TileCollision>();

//     [NaughtyAttributes.OnValueChanged("OnValueChangedCallback")]
//     private float x, z;
//     [HideInInspector]
//     public PlayerControls playerControls;
//     public GameObject playerCamera;

//     private void Update()
//     {
//         if (!AI)
//         {

//             if (Input.GetKeyDown(KeyCode.Space) && !isPulling)
//             {
//                 HandleJump();
//             }

//             if (Input.GetKeyDown(KeyCode.C))
//             {
//                 playerAttack.Hit();
//             }
//             if (Input.GetKeyDown(KeyCode.X))
//             {
//                 playerAttack.HitDown();
//             }
//             // if (Input.GetKeyDown(KeyCode.C))
//             // {
//             //     playerObstacleController.HandlePull();
//             // }       
//         }
//     }
//     public override void FixedUpdateNetwork()
//     {
//         HandleGrounded();
//         HandleWalking();
//         // GetFacingDirection();
//         //IF CHARACTER MOVEMENT BAD UNCOMMENT THE FACING DIRECTION
//         DrawRaycasts();
//         if (!AI)
//             HandleInput();
//         SlideFlags();
//         Fall();
//     }
//     #endregion
//     public Vector2 inputValues;
//     #region Inputs

//     public GameplayInput CurrentInput => _input;
//     private GameplayInput _input;
//     public struct GameplayInput
//     {
//         public float X, Z;

//     }
//     private void HandleInput()
//     {

//         if (AI) return;
//         if (!isPulling && canMove)
//         {
//             if (controls == Controls.Joystick)
//             {
//                 _inputs.X = dynamicJoystick.Horizontal;
//                 _inputs.Z = dynamicJoystick.Vertical;
//                 _inputs.RawX = (int)dynamicJoystick.Horizontal;
//                 _inputs.RawZ = (int)dynamicJoystick.Vertical;
//                 Debug.Log("X :" + _inputs.RawZ);
//             }
//             else if (controls == Controls.Keyboard)
//             {
//                 _inputs.RawX = (int)Input.GetAxisRaw("Horizontal");
//                 _inputs.RawZ = (int)Input.GetAxisRaw("Vertical");
//             }
//             else
//             {
//                 inputValues = playerInput.actions["Move"].ReadValue<Vector2>();
//             }
//             x = _inputs.X;
//             z = _inputs.Z;
//             if (controls == Controls.Joystick_NewInputSystem)
//             {
//                 _inputs.X = inputValues.x;
//                 _inputs.Z = inputValues.y;
//                 _input.X = inputValues.x;
//                 _input.Z = inputValues.y;
//                 _inputs.RawX = (int)inputValues.x;
//                 _inputs.RawZ = (int)inputValues.y;
//             }
//             if (movementType == MovementType.TwoAxis)
//             {
//                 if (Mathf.Abs(_inputs.RawX) > Mathf.Abs(_inputs.RawZ))
//                 {
//                     _inputs.RawZ = 0;
//                 }
//                 else
//                 {
//                     _inputs.RawX = 0;
//                 }
//             }
//             if (_dir != Vector3.zero)
//             {
//                 previousFlashlightDirection = _dir.normalized;
//             }
//             _dir = new Vector3(_inputs.RawX, 0, _inputs.RawZ);

//         }
//     }
//     private void InstantiateFlashlight(Vector3 direction)
//     {

//         // Instantiate flashlight at the player's position with the previous facing direction
//         GameObject flashlightObj = Instantiate(flashlight, transform.position, Quaternion.LookRotation(direction));

//         // Destroy the flashlight after a delay
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
//     #endregion

//     #region Grounding
//     [Title("Detection", TitleColor.Beige, TitleColor.Beige, 2f, 20f)]
//     [Header("Masks")]
//     [SerializeField]
//     LayerMask _groundMask;

//     [SerializeField]
//     public LayerMask _obstacleMask;

//     [SerializeField]
//     public LayerMask _bombObstacleMask;

//     [SerializeField]
//     LayerMask _tileObstacleMask;

//     [Header("Player Grounded Offsets")]

//     private Vector3 _grounderOffset = new Vector3(0, -0.47f, 0);
//     private float _grounderRadius = 0.1f;

//     [Header("Player Obstacle Offsets")]
//     [Header("Detection Flags")]
//     [NaughtyAttributes.ReadOnly]
//     public bool _isAgainstWall;
//     public bool _isBombBlocked;

//     [NaughtyAttributes.ReadOnly]
//     public bool IsGrounded;
//     public bool grounded;

//     [NaughtyAttributes.ReadOnly]
//     [SerializeField]
//     bool isFalling = false;

//     [NaughtyAttributes.ReadOnly]
//     public bool hasRecentlyFallen = false;
//     public Collider[] _ground = new Collider[1];
//     private Collider[] _wall = new Collider[1];
//     private Collider[] _bombDetector = new Collider[1];


//     private float _wallCheckOffset = 0.45f;
//     private float _bombCheckOffset = 0.89f;

//     [SerializeField]
//     private float _wallCheckRadius = 0.12f;

//     [SerializeField]
//     private float _bombCheckRadius = 0.1f;
//     public Vector3 WallDetectionOffset = new Vector3(0, -0.12f, 0);
//     public Vector3 BombDetectionOffset = new Vector3(0, -0.12f, 0);

//     [Header("Objects")]
//     [SerializeField]
//     Tile tile;
//     public Obstacle obstacle;
//     public float fallHeight;

//     // private Vector3 WallDetectPosition => _anim.transform.position + _anim.transform.forward * _wallCheckOffset;
//     public Vector3 WallDetectPosition =>
//         (_anim.transform.position - WallDetectionOffset)
//         + _anim.transform.forward * _wallCheckOffset;
//     public Vector3 BombDetectPosition =>
//         (_anim.transform.position - BombDetectionOffset)
//         + _anim.transform.forward * _bombCheckOffset;
//     public bool forceApplied;
//     private void Fall()
//     {
//         if (isFalling && !hasRecentlyFallen)
//         {
//             hasRecentlyFallen = true;
//             fallHeight = transform.position.y;
//             Friction(false);
//             _anim.SetBool("Falling", true);
//         }
//     }

//     public float rayDistance = 0.99f; // was 0.95f
//     public LayerMask tileLayer;

//     public Vector3 FindNeighbouringTile()
//     {
//         // Assuming the player's facing direction is forward (you might need to adjust this based on your setup)
//         Vector3 facingDirection = transform.forward;
//         tile = FindCurrentTile();
//         // Cast a ray in the facing direction from the current tile position
//         RaycastHit hit;
//         if (Physics.Raycast(tile.transform.position, facingDirection, out hit, rayDistance))
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

//     private void HandleGrounded()
//     {
//         grounded =
//             Physics.OverlapSphereNonAlloc(
//                 transform.position + _grounderOffset,
//                 _grounderRadius,
//                 _ground,
//                 _groundMask
//             ) > 0;
//         var groundedObstacle =
//             Physics.OverlapSphereNonAlloc(
//                 transform.position + _grounderOffset,
//                 _grounderRadius,
//                 _ground,
//                 _obstacleMask
//             ) > 0;
//         if (!IsGrounded && (grounded))
//         {
//             // Debug.Log("Should be nonjumping");
//             Friction(true);
//             _anim.SetBool("Grounded", grounded);
//             IsGrounded = true;
//             if (_isJumping && jumpTimer > jumpCoyoteTime)
//                 _isJumping = false;
//             isFalling = false;
//             forceApplied = false;
//             _anim.SetBool("Grounded", grounded);
//             _anim.SetBool("Falling", false);
//             _isJumping = false;
//             hasRecentlyFallen = false;
//             _hasRecentlyJumped = false;

//             _anim.SetBool("Jumping", false);
//         }
//         else if (IsGrounded && (!grounded))
//         {
//             _anim.SetBool("Grounded", grounded);
//             IsGrounded = false;
//             if (!_hasRecentlyJumped)
//             {
//                 isFalling = true;
//                 Fall();
//             }
//         }
//         _isAgainstWall =
//             Physics.OverlapSphereNonAlloc(
//                 WallDetectPosition,
//                 _wallCheckRadius,
//                 _wall,
//                 _obstacleMask
//             ) > 0;
//         _isBombBlocked =
//             Physics.OverlapSphereNonAlloc(
//                 BombDetectPosition,
//                 _bombCheckRadius,
//                 _bombDetector,
//                 _bombObstacleMask
//             ) > 0;
//         if (IsGrounded && grounded && _anim.GetBool("Jumping") && !_hasRecentlyJumped)
//         {
//             Debug.Log("Setting Jumping to false");
//             _isJumping = false;
//             _anim.SetBool("Jumping", false);
//         }

//         if (isAgainstWall && _isJumping)
//             canMove = false;
//     }

//     public Obstacle FindObstacle()
//     {
//         if (_wall[0] != null)
//             return _wall[0].gameObject.GetComponent<Obstacle>();
//         else return null;
//     }

//     private void DrawGrounderGizmos()
//     {
//         Gizmos.color = Color.black;
//         Gizmos.DrawWireSphere(transform.position + _grounderOffset, _grounderRadius);
//     }

//     private void DrawWallSlideGizmos()
//     {
//         Gizmos.color = Color.red;
//         Gizmos.DrawWireSphere(WallDetectPosition, _wallCheckRadius);
//     }

//     // private void OnDrawGizmos()
//     // {
//     //     DrawSlideSpheres();
//     //     DrawGrounderGizmos();
//     //     DrawWallSlideGizmos();
//     // }
//     #endregion

//     #region Walking
//     [Title("Walking", TitleColor.Red, TitleColor.Red, 2f, 20f)]
//     [Header("Flags")]
//     [TetraCreations.Attributes.ReadOnly]
//     [SerializeField]
//     public bool canMove;

//     [Header("Variables")]
//     [SerializeField]
//     public float _walkSpeed = 2;

//     public float obstacleAvoidanceRange = 0.06f;

//     [NaughtyAttributes.ReadOnly]
//     public Vector3 _dir;

//     public Vector3 GetFacingDirection()
//     {
//         float currentRotation = transform.eulerAngles.y;
//         if (currentRotation == 270)
//             return new Vector3(-1, 0, 0);
//         if (currentRotation == 90)
//             return new Vector3(1, 0, 0);
//         if (currentRotation == 0)
//             return new Vector3(0, 0, 1);
//         if (currentRotation == 180)
//             return new Vector3(0, 0, -1);
//         else
//             return Vector3.zero;
//     }

//     public float raycastDistance = 1.0f;

//     public float avoidanceMultiplier;
//     public float raycastAngle;
//     public Vector3 leftEyeOffset = new Vector3(0.025f, 0, 0),
//         rightEyeOffset = new Vector3(-0.025f, 0, 0),
//         climbRayOffset = new Vector3(0, 0.45f, 0),
//         behindObstacleOffset;
//     public bool pullObstacleFlag;
//     public LayerMask _behindObstacleMask;

//     private void DrawRaycasts()
//     {
//         /**
//         **Obstacle avoidance
//         */
//         _leftEyeDirection = Quaternion.Euler(0, -raycastAngle, 0) * transform.forward;
//         _rightEyeDirection = Quaternion.Euler(0, raycastAngle, 0) * transform.forward;
// #if UNITY_EDITOR
//         Debug.DrawRay(
//             transform.position + leftEyeOffset,
//             _leftEyeDirection * raycastDistance,
//             Color.blue
//         );
//         Debug.DrawRay(
//             transform.position + rightEyeOffset,
//             _rightEyeDirection * raycastDistance,
//             Color.blue
//         );
// #endif

//         /**
//        **Obstacle behind player checker
//        */
//         Vector3 obstacleBehindWhilePulling = Quaternion.Euler(0, 0, 0) * (transform.forward * -1);
// #if UNITY_EDITOR
//         Debug.DrawRay(
//             transform.position + behindObstacleOffset,
//             obstacleBehindWhilePulling * raycastDistance,
//             Color.red
//         );
// #endif
//         Ray rayBack = new Ray(transform.position, transform.forward * -1);
//         RaycastHit hitBack;
//         obstacleBehind = Physics.Raycast(rayBack, out hitBack, 0.6f, _behindObstacleMask);

//         /**
//        **Forward Cast
//        */
//         Ray ray = new Ray(transform.position, transform.forward);
//         RaycastHit hit;

//         if (playerControls == null || AI)
//             return;
//         pullObstacleFlag =
//             Physics.Raycast(ray, out hit, 0.5f, _obstacleMask)
//             || Physics.Raycast(ray, out hit, 0.5f, collectibleMask);
//         // Debug.Log("pullObstacleFlag : " + pullObstacleFlag);
//         playerControls.Interactable(pullObstacleFlag);
//         if (playerControls == null) Debug.LogError("Playercontrols are null");
//         if (playerControls.levelGoal == null) Debug.LogError("PlayerLevelGoal is null");
//         if (playerControls == null) Debug.LogError("Playercontrols are null");
//         if (playerControls != null &&
//             !playerControls.isPulling
//             && playerControls.levelGoal.levelType == LevelGoal.LevelType.Pull
//         )
//             playerControls.hintPull.gameObject.SetActive(pullObstacleFlag);
//     }

//     [SerializeField]
//     public LayerMask collectibleMask;
//     private bool _leftEyeHitObstacle,
//         _rightEyeHitObstacle;
//     private Vector3 _leftEyeDirection,
//         _rightEyeDirection;

//     public Vector3 previousMoveDirection;
//     public Vector3 currentMoveDirection;
//     public void HandleWalking()
//     {
//         if (!canMove)
//             return;
//         Vector3 moveDirection = _dir.normalized;
//         currentMoveDirection = _dir.normalized;
//         if (_dir != Vector3.zero)
//             transform.forward = _dir;

//         Vector3 avoidanceVector = Vector3.zero;
//         bool avoidObstacle = false;

//         _leftEyeHitObstacle = Physics.Raycast(
//             transform.position + leftEyeOffset,
//             _leftEyeDirection,
//             out RaycastHit leftEyeHit,
//             raycastDistance,
//             _tileObstacleMask
//         );
//         _rightEyeHitObstacle = Physics.Raycast(
//             transform.position + rightEyeOffset,
//             _rightEyeDirection,
//             out RaycastHit rightEyeHit,
//             raycastDistance,
//             _tileObstacleMask
//         );

//         if (_leftEyeHitObstacle && _rightEyeHitObstacle)
//         {
//             // Both eyes hit obstacles, we need to steer away from both
//             Vector3 leftAvoidanceVector = Vector3.Cross(leftEyeHit.normal, Vector3.up).normalized;
//             Vector3 rightAvoidanceVector = Vector3.Cross(rightEyeHit.normal, Vector3.up).normalized;

//             avoidanceVector = (leftAvoidanceVector + rightAvoidanceVector).normalized;
//             avoidObstacle = true;
//         }
//         else if (_leftEyeHitObstacle)
//         {
//             // Only the left eye hit an obstacle
//             avoidanceVector = Vector3.Cross(leftEyeHit.normal, Vector3.up).normalized;
//             avoidObstacle = true;
//         }
//         else if (_rightEyeHitObstacle)
//         {
//             // Only the right eye hit an obstacle
//             avoidanceVector = Vector3.Cross(rightEyeHit.normal, Vector3.up).normalized;
//             avoidObstacle = true;
//         }

//         if (avoidObstacle && !isFalling && _dir != Vector3.zero)
//         {
//             // Apply avoidance force to change direction
//             Vector3 finalMovement = _dir.normalized + avoidanceVector * avoidanceMultiplier;
//             Vector3 nextPosition =
//                 transform.position + finalMovement * player.MoveSpeed * Time.fixedDeltaTime;
//             _rb.MovePosition(nextPosition);
//             if (player.MoveSpeed >= 3)
//             {
//                 _anim.SetBool("Running", _dir != Vector3.zero && IsGrounded);
//                 _anim.SetBool("Walking", false);
//             }
//             else
//             {
//                 _anim.SetBool("Walking", _dir != Vector3.zero && IsGrounded);
//                 _anim.SetBool("Running", false);
//             }
//             _anim.SetBool("AFK", false);
//         }
//         else
//         {
//             // No obstacles were hit, move in the input direction
//             Vector3 finalMovement = _dir.normalized;
//             Vector3 nextPosition =
//                 transform.position + finalMovement * player.MoveSpeed * Time.fixedDeltaTime;
//             _rb.MovePosition(nextPosition);
//             if (player.MoveSpeed >= 3)
//             {
//                 _anim.SetBool("Running", _dir != Vector3.zero && IsGrounded);
//                 _anim.SetBool("Walking", false);
//             }
//             else
//             {
//                 _anim.SetBool("Walking", _dir != Vector3.zero && IsGrounded);
//                 _anim.SetBool("Running", false);
//             }
//             _anim.SetBool("AFK", false);
//         }
//         if (_dir == Vector3.zero && !_isJumping && !isPushing && !isPulling && !isFalling)
//         {
//             _anim.SetBool("AFK", true);
//             LookAnimation();
//         }
//         else
//         {
//             _anim.SetBool("AFK", false);
//             _anim.SetBool("LookLeft", false);
//             _anim.SetBool("LookRight", false);
//         }
//         if (moveDirection != Vector3.zero && !_isJumping && IsGrounded)
//         {
//             // AudioManager.Instance.PlaySoundWithDelay(0.42f, 6);
//             // StartCoroutine(PlaySoundWithDelay(0.42f));
//             if (!AI) AudioManager.Instance.StartPlayerFootsteps(transform.position);
//         }
//         else
//         {
//             // If the player has stopped moving, stop the walking sound
//             if (!AI) AudioManager.Instance.StopPlayerFootsteps();
//         }

//         if (_dir != previousFlashlightDirection && _dir != Vector3.zero && GameManager.Instance.DarkLevel && flashlight != null && !AI)
//             InstantiateFlashlight(previousFlashlightDirection);
//         if (currentMoveDirection != previousMoveDirection && playerObstacleController != null)
//         {
//             playerObstacleController.StopPush();
//             if (playerObstacleController != null && _dir != Vector3.zero) // Add _dir != Vector3.zero check
//             {
//                 Debug.Log("Code 0");
//                 Debug.Log("Setting push direction to true");
//                 playerObstacleController.pushDirectionChanged = true;
//             }
//         }

//         if (_dir != Vector3.zero && playerObstacleController != null)
//         {
//             previousMoveDirection = currentMoveDirection;
//         }
//     }
//     private float lastSoundPlayTime;
//     private void PlaySoundWithDelay(float delay)
//     {
//         // yield return new WaitForSeconds(delay);
//         // AudioManager.Instance.PlayPlayerSound("walk");

//         float currentTime = Time.time;

//         if (currentTime - lastSoundPlayTime >= delay)
//         {
//             if (!AI) AudioManager.Instance.PlayPlayerSound("walk", transform.position);
//             lastSoundPlayTime = currentTime;
//         }
//     }
//     public void LookAnimation()
//     {
//         Vector3 cameraDirection = playerCamera.transform.position - transform.position;
//         if (Vector3.Dot(transform.forward, cameraDirection) > 0)
//         {
//             Vector3 localCameraDirection = transform.InverseTransformDirection(cameraDirection);
//             if (localCameraDirection.x >= 0)
//             {
//                 _anim.SetBool("LookLeft", false);
//                 _anim.SetBool("LookRight", true);
//             }
//             else
//             {
//                 _anim.SetBool("LookLeft", true);
//                 _anim.SetBool("LookRight", false);
//             }
//         }
//         else
//         {
//             _anim.SetBool("LookLeft", false);
//             _anim.SetBool("LookRight", false);
//         }
//     }
//     #endregion

//     #region Jumping
//     [Title("Jumping", TitleColor.Green, TitleColor.Green, 2f, 20f)]
//     [SerializeField]
//     float nudgeDistance;

//     [SerializeField]
//     float _jumpForce = 5.8f;

//     [NaughtyAttributes.ReadOnly]
//     [SerializeField]
//     public bool _isJumping;

//     [SerializeField]
//     float jumpTimer = 0;

//     [SerializeField]
//     float jumpCoyoteTime = 0.14f;

//     [NaughtyAttributes.ReadOnly]
//     [SerializeField]
//     private bool _hasRecentlyJumped;
//     private bool female;
//     public void HandleJump()
//     {
//         if (isPulling) return;
//         // Debug.Log("Is Grounded " + IsGrounded + " is jumping " + _isJumping);
//         if (IsGrounded && !_isJumping)
//         {
//             Jump();
//             // Debug.Log("Entered Jump");
//         }
//         void Jump()
//         {
//             if (!_hasRecentlyJumped)
//             {
//                 Friction(false);
//                 jumpParticle.Play();
//                 _isJumping = true;
//                 _hasRecentlyJumped = true;

//                 if (isPushing)
//                 {
//                     Debug.Log("It's currently pushing");
//                     if (playerObstacleController.pushObstacle != null)
//                     {
//                         playerObstacleController.pushObstacle.isBeingPushed = false;
//                         playerObstacleController.pushObstacle = null;
//                     }
//                 }
//                 // Vector3 nudgeDirection = -transform.forward * nudgeDistance;
//                 // transform.position += nudgeDirection;
//                 _rb.linearVelocity = Vector3.zero;
//                 _rb.angularVelocity = Vector3.zero;
//                 _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, _rb.linearVelocity.z);
//                 _anim.SetBool("Jumping", true);
//                 AudioManager.Instance.PlayJumpSound(female, transform.position);
//             }
//         }
//     }
//     public void HitJump()
//     {
//         _rb.linearVelocity = Vector3.zero;
//         _rb.linearVelocity = new Vector3(0, _jumpForce * 0.8f, 0);
//     }

//     #endregion

//     #region Push
//     [Title("Push", TitleColor.Indigo, TitleColor.Indigo, 2f, 20f)]

//     [NaughtyAttributes.ReadOnly]
//     [SerializeField]
//     public bool isPushing;

//     [SerializeField]
//     public bool canPush = true;

//     [NaughtyAttributes.ReadOnly]
//     [SerializeField]
//     public bool isPulling;

//     [NaughtyAttributes.ReadOnly]
//     [SerializeField]
//     bool isAgainstWall;

//     #endregion

//     #region Pull
//     [SerializeField] public bool recentlyHitWall;
//     public bool obstacleBehind;
//     private Obstacle obstacleBackwards;
//     public bool _pullButtonHeld;
//     public bool _pullButtonReleased;

//     public void SetPullConstraints(Obstacle obstacle)
//     {
//         if (_pullButtonHeld)
//         {
//             isPulling = true;
//             isPushing = false;
//             if (obstacle != null && obstacle.grounded)
//                 if (obstacle != null)
//                 {
//                     canMove = false;
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
//             // Debug.Log("Setting movespeed");
//             if (!player.hasSpeedBuff)
//                 player.MoveSpeed = player.StartingMoveSpeed;
//             else
//                 player.MoveSpeed = player.buffedSpeed;
//             if (!playerAttack.hittingDown)
//                 canMove = true;
//             _pullButtonReleased = false;
//             _rb.isKinematic = false;
//             _rb.constraints = _originalConstraints;
//             isPulling = false;
//             if (obstacle != null)
//             {
//                 obstacle.recentlyPulled = true;
//                 ResetObstacle(obstacle, "Key was let go");
//             }
//             _anim.SetBool("Pull", false);
//         }
//     }

//     #endregion
//     public float tileRayLength = 50f;

//     public Tile FindCurrentTile()
//     {
//         // Shoot a ray down from the player's position, filtering by the "Tile" layer
//         RaycastHit hit;
//         if (Physics.Raycast(transform.position, Vector3.down, out hit, tileRayLength, tileLayer))
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

//     [System.Serializable]
//     public struct FrameInputs
//     {
//         public float X,
//             Z;
//         public int RawX,
//             RawZ;
//     }



//     public enum TileCollisionType
//     {
//         On,
//         Off
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
//             obstacle = null;
//         }
//         if (obstacleBackwards != null)
//             obstacleBackwards = null;
//     }

//     public Vector3 forwardOffset = new Vector3(0, 0.48f, 0),
//         backOffset = new Vector3(0, 0.48f, 0),
//         leftOffset,
//         rightOffset;
//     public float frontoffs = 0.3f,
//         backoffs = -0.3f;
//     public float slideCheckerRadius = 0.025f;
//     private Vector3 Front =>
//         (_anim.transform.position - forwardOffset) + _anim.transform.forward * frontoffs;
//     private Vector3 Back =>
//         (_anim.transform.position - backOffset) + _anim.transform.forward * backoffs;

//     public void DrawSlideSpheres()
//     {
//         Gizmos.color = Color.cyan;
//         Gizmos.DrawWireSphere(Front, slideCheckerRadius);
//         Gizmos.DrawWireSphere(Back, slideCheckerRadius);
//     }

//     public bool slideForward,
//         slideBack,
//         slideLeft,
//         slideRight;
//     public float slideForce = 65f;
//     public Collider[] _slideForward = new Collider[1];
//     public Collider[] _slideBack = new Collider[1];
//     public Collider[] _slideLeft = new Collider[1];
//     public Collider[] _slideRight = new Collider[1];
//     public float slideAngle = 25f;

//     public void SlideFlags()
//     {
//         slideForward =
//             Physics.OverlapSphereNonAlloc(Front, slideCheckerRadius, _slideForward, _obstacleMask)
//             > 0;
//         slideBack =
//             Physics.OverlapSphereNonAlloc(Back, slideCheckerRadius, _slideBack, _obstacleMask) > 0;

//         if (
//             (slideForward || slideBack || slideLeft || slideRight)
//             && !grounded
//             && isFalling
//             && _dir == Vector3.zero
//         )
//         {
//             Vector3 slideDirection = Vector3.zero;
//             Vector3 forceDirection = Quaternion.Euler(slideAngle, 0f, 0f) * transform.forward;

//             if (slideForward)
//                 slideDirection += -transform.forward;
//             if (slideBack)
//                 slideDirection += transform.forward;

//             Vector3 totalForce = (slideDirection * slideForce) + (forceDirection * slideForce);
//             _rb.AddForce(totalForce, ForceMode.Impulse);
//         }
//     }

//     public bool changedToJump,
//         changedToGround;

//     public void Friction(bool mode)
//     {
//         if (mode && !changedToGround)
//         {
//             changedToGround = true;
//             changedToJump = false;
//             playerCollider.sharedMaterial.staticFriction = 1;
//             playerCollider.sharedMaterial.dynamicFriction = 1;
//         }
//         else if (!mode && !changedToJump)
//         {
//             changedToGround = false;
//             changedToJump = true;
//             playerCollider.sharedMaterial.staticFriction = 0;
//             playerCollider.sharedMaterial.dynamicFriction = 0;
//         }
//     }
//     CinemachineFreeLook cinemachineFreeLook;
//     CinemachineBasicMultiChannelPerlin cinemachinePerlin;
//     public void ShakeCamera()
//     {
//         if (cinemachineFreeLook == null)
//             cinemachineFreeLook = playerCamera.GetComponent<CinemachineFreeLook>();
//         if (cinemachinePerlin == null)
//             cinemachinePerlin = cinemachineFreeLook.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

//         cinemachinePerlin.m_AmplitudeGain = 3f;
//         StartCoroutine(ShakeWaitTime(0.3f));

//     }
//     public IEnumerator ShakeWaitTime(float shakeTime)
//     {
//         Debug.Log("Shaking cam");
//         yield return new WaitForSeconds(shakeTime);
//         if (cinemachinePerlin != null) cinemachinePerlin.m_AmplitudeGain = 0f;
//     }
// }

// public enum Controls
// {
//     Keyboard,
//     Joystick,
//     Joystick_NewInputSystem
// }

// public enum MovementType
// {
//     TwoAxis,
//     FreeMove
// }

