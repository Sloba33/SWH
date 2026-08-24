using System.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using UnityEngine;


public class PlayerObstacleController : MonoBehaviour
{
    // separate obstacle shit a bit more
    public bool lockMidAirPush;
    private PlayerMovement playerMovement;
    PlayerController playerController;
    Player player;
    [SerializeField] Vector3 movementDirection;
    [SerializeField] Vector3 previousPushDirection;
    [SerializeField] Vector3 pullDirection;
    [SerializeField] Rigidbody _rb;
    [SerializeField] Animator _anim;
    [SerializeField] public Obstacle pushObstacle, previousPushObstacle, pullObstacle, previousPullObstacle;
    [SerializeField] bool diff;
    [SerializeField] float pushSpeed;
    public Vector3 previousMoveDirection;
    public Vector3 currentMoveDirection;

    // Add this field for the player's collider
    [SerializeField] private Collider playerCollider;

    // Smooth positioning variables
    private bool isRepositioning = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    [SerializeField] private float repositionSpeed = 15f; // Adjust this to control repositioning speed
    [SerializeField] private float pullRepositionSpeed = 25f; // Faster than push
    [SerializeField] private float pullDistance = 0.75f; // Faster than push


    private float repositionProgress = 0f;

    private void Start()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        player = GetComponent<Player>();
        pushSpeed = player.StartingMoveSpeed / 2;
        playerMovement = GetComponent<PlayerMovement>();

        // Ensure playerCollider is assigned. If not set in Inspector, try to find it.
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>(); // Gets any collider on the player object
            if (playerCollider == null)
            {
                Debug.LogError("PlayerObstacleController: No Collider found on player or assigned. Snapping might be incorrect.");
            }
        }
    }
    private void ControlPlayerFallWhilePulling()
    {
        if (playerController.isPulling && pullObstacle != null && pullObstacle.isFalling)
        {
            // **** NEW VALIDATION CHECK ****
            // Check if the obstacle has fallen too far below the player
            float yDifference = transform.position.y - pullObstacle.transform.position.y;
            if (yDifference > 0.8f) // Assuming player height is ~1.0, 0.8f seems like a safe threshold
            {
                Debug.Log("Obstacle fell too far during repositioning. Stopping pull.");
                StopPull(); // This will also stop the repositioning
                return;
            }
            // ****************************

            // Prevent syncing Y if player is standing on anything
            if (!playerMovement.IsGrounded)
            {
                Vector3 pos = transform.position;
                pos.y = pullObstacle.transform.position.y;
                transform.position = pos;
            }
        }
    }

    private bool pullConstraintsReset;
    private void FixedUpdate()
    {
        movementDirection = playerController._movement.CurrentMoveDirection;

        // Handle smooth repositioning
        if (isRepositioning)
        {
            HandleSmoothRepositioning();
            ControlPlayerFallWhilePulling(); // Keep player Y synced *during* repositioning
        }

        if (!playerController.isPushing && previousPushObstacle != null)
        {
            previousPushObstacle.ResetObstacle();
            previousPushObstacle = null;
        }
        HandlePush();

        // Removed: playerController._movement.SetPullConstraints(pullObstacle);

        if (playerController.isPulling && !playerController.pullButtonReleased)
        {
            // This is the main "Pulling" loop

            if (!isRepositioning && !_rb.isKinematic)
            {
                // State: Pull button is held, but we are not in position.
                // Try to initiate the pull (which starts repositioning).
                HandlePull_Initiate(); // Renamed from HandlePull
            }
            else if (!isRepositioning && _rb.isKinematic)
            {
                // State: Pull button is held, we are in position and kinematic.
                // This is the active "Pulling" state.
                HandlePull_Continue();
            }
            // else if (isRepositioning)
            // {
            //    // State: Pull button is held, we are moving into position.
            //    // Handled by the isRepositioning block at the top of FixedUpdate.
            // }
        }

        if (!pullConstraintsReset && !playerController.pullButtonHeld && playerController.pullButtonReleased)
        {
            Debug.Log("Pull stopped");
            pullConstraintsReset = true;
            StopPull();

        }
    }

    private void HandleSmoothRepositioning()
    {
        repositionProgress += currentRepositionSpeed * Time.fixedDeltaTime;

        if (repositionProgress >= 1f)
        {
            transform.position = targetPosition;
            isRepositioning = false;
            repositionProgress = 0f;

            // **** NEW LOGIC ****
            // Finished repositioning, now lock player for pulling
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            // *******************
        }
        else
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, repositionProgress);
        }
    }
    private void StartSmoothRepositioning(Vector3 newTargetPosition, float customRepositionSpeed = -1f)
    {
        if (!isRepositioning || Vector3.Distance(targetPosition, newTargetPosition) > 0.1f)
        {
            startPosition = transform.position;
            targetPosition = newTargetPosition;
            isRepositioning = true;
            repositionProgress = 0f;

            // Use custom speed if provided, otherwise use default
            currentRepositionSpeed = customRepositionSpeed > 0 ? customRepositionSpeed : repositionSpeed;

            Debug.Log("Starting smooth repositioning to: " + targetPosition + " with speed: " + currentRepositionSpeed);
        }
    }
    private float currentRepositionSpeed;

    // Modify HandleSmoothRepositioning to use currentRepositionSpeed

    private float lastLandTime = 0f;
    private const float PUSH_RE_EVAL_DELAY_AFTER_LAND = 0.2f; // Time to delay full push re-evaluation after landing
    public void HandlePush()
    {
        // Debug.Log("[HandlePush] START");
        // Debug.Log($"isPulling: {playerController.isPulling}, justLanded: {playerMovement.justLanded}, justJumpedOutOfPush: {playerMovement.justJumpedOutOfPush}, linearVelocity.y: {_rb.linearVelocity.y}");
        if (playerController.isPulling || isRepositioning) // Do not allow push while pulling or repositioning to pull
            return;

        if (playerMovement.justLanded && lastLandTime == 0f)
            lastLandTime = Time.time;

        if (lastLandTime != 0f && (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
            lastLandTime = 0f;

        if (playerMovement.justJumpedOutOfPush && _rb.linearVelocity.y > 0.1f)
        {
            if (pushObstacle != null) pushObstacle.ResetObstacle();
            StopPush();
            return;
        }

        bool canPotentiallyPush = playerController._movement.IsAgainstWall && movementDirection != Vector3.zero;
        // Debug.Log($"canPotentiallyPush: {canPotentiallyPush}, IsAgainstWall: {playerController._movement.IsAgainstWall}, moveDir: {movementDirection}");
        if (!playerController.isPushing && !canPotentiallyPush)
        {
            // Debug.Log("[HandlePush] Early exit: not pushing + can't potentially push");
            if (lastLandTime == 0f || (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
            {
                previousPushDirection = Vector3.zero;
                if (pushObstacle != null) pushObstacle.ResetObstacle();
                StopPush();
                return;
            }
            else
            {
                if (pushObstacle != null) pushObstacle.ResetObstacle();
                StopPush();
                return;
            }
        }

        if (canPotentiallyPush)
        {
            Obstacle foundObstacle = playerController.FindObstacle();
            Debug.Log($"FindObstacle returned: {(foundObstacle ? foundObstacle.name : "null")}");
            if (foundObstacle != null)
            {
                if (pushObstacle == null || pushObstacle != foundObstacle)
                {
                    if (pushObstacle != null) pushObstacle.ResetObstacle();
                    pushObstacle = foundObstacle;
                    lockMidAirPush = true;
                    Debug.Log($"pushObstacle assigned: {pushObstacle.name}, grounded: {pushObstacle.grounded}, falling: {pushObstacle.isFalling}, pushable: {pushObstacle.isPushable}");
                }
                else Debug.Log("pushObstacle is null after assignment.");
            }
            else if (pushObstacle != null && playerController.isPushing)
            {
                pushObstacle.ResetObstacle();
                StopPush();
                return;
            }
            else
            {
                pushObstacle = null;
            }

            if (pushObstacle == null)
            {
                previousPushDirection = Vector3.zero;
                StopPush();
                return;
            }

            pushObstacle.SphereFlags();
            if (!pushObstacle.isPushable)
            {
                previousPushDirection = Vector3.zero;
                pushObstacle.ResetObstacle();
                StopPush();
                return;
            }

            playerController.isPushing = true;

            if (previousPushObstacle != pushObstacle)
            {
                if (previousPushObstacle != null) previousPushObstacle.ResetObstacle();
                previousPushObstacle = pushObstacle;
            }

            pushObstacle.SphereFlags();
            bool Moveable = pushObstacle.CheckObstaclesAround(movementDirection);
            if (!Moveable || !playerController._movement.CanPush)
            {
                Debug.Log("[HandlePush] Obstacle not movable in that direction or can't push.");
                pushObstacle.ResetObstacle();
                StopPush();
                return;
            }
            Debug.Log($"pushObstacle Moveable: {Moveable}, pushabilityDelayed: {pushObstacle.pushabilityDelayed}");
            if (playerController._movement.hasRecentlyFallen)
                diff = Mathf.Round(playerController._movement.fallHeight) - pushObstacle.transform.position.y >= 0;
            else
                diff = true;
            Debug.Log($"Centering push? Conditions => CanPush: {playerController._movement.CanPush}, Moveable: {Moveable}, diff: {diff}, !delayed: {!pushObstacle.pushabilityDelayed}");
            if (pushObstacle != null && movementDirection != Vector3.zero &&
                playerController._movement.CanPush && Moveable && diff && !pushObstacle.pushabilityDelayed)
            {
                if (playerCollider == null)
                {
                    StopPush();
                    return;
                }

                Vector3 normalizedMovementDirection = movementDirection.normalized;
                float playerHalfSize = ((CapsuleCollider)playerCollider).radius;
                float obstacleHalfSize = 0.5f;
                float combinedSizeAndOffset = obstacleHalfSize + playerHalfSize + manualPushDistance;

                Vector3 targetPlayerPosition = pushObstacle.transform.position;

                if (Mathf.Abs(normalizedMovementDirection.x) > 0.01f)
                {
                    targetPlayerPosition.x = pushObstacle.transform.position.x - (normalizedMovementDirection.x * combinedSizeAndOffset);
                    targetPlayerPosition.z = pushObstacle.transform.position.z;
                }
                else if (Mathf.Abs(normalizedMovementDirection.z) > 0.01f)
                {
                    targetPlayerPosition.z = pushObstacle.transform.position.z - (normalizedMovementDirection.z * combinedSizeAndOffset);
                    targetPlayerPosition.x = pushObstacle.transform.position.x;
                }

                targetPlayerPosition.y = pushObstacle.transform.position.y;

                Vector3 current = _rb.position;
                Vector3 target = current;
                Vector3 obstaclePos = pushObstacle.transform.position;
                Vector3 dir = movementDirection.normalized;


                if (Mathf.Abs(dir.z) > 0.01f)
                {
                    target.x = Mathf.Lerp(current.x, obstaclePos.x, pushCenteringSmoothFactor);
                }

                if (Mathf.Abs(dir.x) > 0.01f)
                {
                    target.z = Mathf.Lerp(current.z, obstaclePos.z, pushCenteringSmoothFactor);
                }


                target.y = obstaclePos.y;


                Vector3 velocity = (target - current) / Time.fixedDeltaTime;
                _rb.linearVelocity = new Vector3(velocity.x, _rb.linearVelocity.y, velocity.z);

                currentMoveDirection = movementDirection;


                if (previousMoveDirection == Vector3.zero && currentMoveDirection != Vector3.zero)
                {
                    pushDirectionChanged = true;
                    if (pushDirectionChanged)
                    {
                        pushDirectionChanged = false;
                        previousMoveDirection = currentMoveDirection;
                        return;
                    }
                }

                if (currentMoveDirection != previousMoveDirection && pushObstacle != null &&
                    playerController._movement.CanPush && pushObstacle.Movable(movementDirection))
                {
                    pushDirectionChanged = true;
                    if (pushDirectionChanged)
                    {
                        pushDirectionChanged = false;
                        previousMoveDirection = currentMoveDirection;
                        return;
                    }
                }

                previousPushDirection = movementDirection;

                if (pushObstacle != null && Moveable && playerController._movement.CanPush)
                    Push();

                previousMoveDirection = currentMoveDirection;
            }

            else if (!pushObstacle.grounded && pushObstacle.isFalling && !playerController._movement.IsGrounded && Moveable)
            {

                Debug.Log("[HandlePush] Mid-air fallback branch hit — pushing airborne obstacle");
                Push();
            }
            else if (!diff && Moveable)
            {
                Push();
            }
            else
            {
                Debug.Log("[HandlePush] FINAL fallback - stopping push");
                pushObstacle?.ResetObstacle();
                StopPush();
            }
        }
        else
        {
            Debug.Log("[HandlePush] FINAL fallback2 - stopping push");
            if (lastLandTime == 0f || (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
            {
                if (pushObstacle != null) pushObstacle.ResetObstacle();
                StopPush();
            }
            else
            {
                if (pushObstacle != null) pushObstacle.ResetObstacle();
                StopPush();
                return;
            }
        }
        Debug.Log("[HandlePush] END");
        lockMidAirPush = false;
    }

    [SerializeField] private float pushCenteringSmoothFactor = 0.15f; // Adjust this value in Inspector (0 to 1)
    public bool pushDirectionChanged = false;
    void Push()
    {
        if (pushDirectionChanged)
        {
            pushDirectionChanged = false;
            return;
        }

        if (pushObstacle == null)
        {
            Debug.LogWarning("[Push] pushObstacle is NULL");
            return;
        }

        playerController.isPushing = true;
        pushObstacle.isBeingPushed = true;
        pushObstacle.wasRecentlyPushed = true;

        Debug.Log("[Push] Starting coordinated push on obstacle: " + pushObstacle.name);

        // StartCoroutine(CoordinatedPushMovement());
        Vector3 dir = new Vector3(movementDirection.x, pushObstacle.transform.position.y, movementDirection.z);
        pushObstacle.PushObstacle(dir, player.PushAndPullSpeed(pushObstacle.Weight));

        _anim.SetBool("Push", true);
        _anim.SetBool("Idle", false);
        if (pushObstacle.obstacleAudioType != ObstacleAudioType.Plastic)
        {

            AudioManager.Instance.PlayObstacleSound_Move(pushObstacle.obstacleAudioType, transform.position);
        }
        else
        {
            AudioManager.Instance.PlayObstacleSound_Move(ObstacleAudioType.Wood, transform.position);

        }
        
    }

    private IEnumerator CoordinatedPushMovement()
    {
        while (playerController.isPushing)
        {
            if (pushObstacle == null)
            {
                Debug.LogWarning("[PushCoroutine] pushObstacle became null. Exiting push.");
                StopPush(); // clean up if needed
                yield break;
            }

            Vector3 moveDirection = movementDirection.normalized;
            float speed = player.PushAndPullSpeed(pushObstacle.Weight);

            if (moveDirection != Vector3.zero)
            {
                yield return new WaitForFixedUpdate();

                if (pushObstacle == null)
                {
                    Debug.LogWarning("[PushCoroutine] Obstacle null after WaitForFixedUpdate.");
                    StopPush();
                    yield break;
                }

                Vector3 deltaMovement = moveDirection * speed * Time.fixedDeltaTime;
                Vector3 playerTarget = transform.position + deltaMovement;
                Vector3 obstacleTarget = pushObstacle.transform.position + deltaMovement;

                if (pushObstacle.isFalling && !pushObstacle.grounded)
                    playerTarget.y = obstacleTarget.y;

                _rb.MovePosition(playerTarget);
                pushObstacle._rb.MovePosition(obstacleTarget);
            }
            else
            {
                yield return null;
            }
        }

        StopPush();
    }

    public void StopPush()
    {
        if (lockMidAirPush)
        {
            // Debug.LogWarning("[StopPush] Prevented push stop — lockMidAirPush active");
            return;
        }
        if (!playerController.AI)
            AudioManager.Instance.StopObstacleSound_Move();

        if (pushObstacle != null && pushObstacle.isBeingPulled)
            return;

        _anim.SetBool("Push", false);

        if (pushObstacle != null)
        {

            pushObstacle.ResetObstacle();


            if (!pushObstacle._rb.isKinematic)
                pushObstacle._rb.linearVelocity = Vector3.zero;


            pushObstacle.currentlyUsedPlayerConrtoller = null;
        }
        pushObstacle = null;
        previousPushObstacle = null;
        previousPushDirection = Vector3.zero;
        previousMoveDirection = Vector3.zero;
        playerController.isPushing = false;

        pushDirectionChanged = false;


        isRepositioning = false;
        repositionProgress = 0f;

        return;
    }
    bool collected = false;
    private IEnumerator SetCollected()
    {
        yield return new WaitForSeconds(0.5f);
        collected = false;
    }
    public float delayTimer;
    private bool started, ended;

    /// <summary>
    /// Checks for a pullable obstacle and starts the repositioning process.
    /// </summary>
    /// 
    /// 
    public void HandlePull_Initiate()
    {

        PlayerAttack playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null && playerAttack.IsAttacking())
        {
            Debug.Log("Cannot initiate pull while attacking");
            return;
        }
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hitCollectible, 0.5f, playerController._movement._collectibleMask))
        {
            if (hitCollectible.transform.TryGetComponent<CollectibleItem>(out var collectibleItem))
            {
                Debug.Log("Looted collectible");

                if (!collectibleItem.isCollected)
                {
                    collectibleItem.Collect(playerController);
                    collectibleItem.isCollected = true;
                }
            }
        }
        else if (Physics.Raycast(ray, out RaycastHit hit, 0.5f, playerController._movement._obstacleMask))
        {
            pullObstacle = hit.transform.GetComponent<Obstacle>();

            if (pullObstacle != null && pullObstacle.grounded && pullObstacle.isPullable)
            {
                pullObstacle.SphereFlags();

                // We know from FixedUpdate that isPulling is true, and we are not repositioning or kinematic.
                playerController._movement.SetPullConstraints(pullObstacle); // Set flags (IsPulling=true, CanMove=false)

                pullObstacle.playerController = playerController;
                pullDirection = -playerController._movement.GetFacingDirection();

                if (playerCollider == null)
                {
                    Debug.LogError("Player collider not assigned. Cannot snap during pull.");
                    StopPull();
                    return;
                }

                Vector3 normalizedPullDirection = pullDirection.normalized;
                float obstacleHalfSize = 0.5f;
                float playerHalfSize = ((CapsuleCollider)playerCollider).radius;

                Vector3 targetPlayerPosition = pullObstacle.transform.position +
                    (normalizedPullDirection * (obstacleHalfSize + playerHalfSize + pullDistance));

                // Ensure target Y is player's current Y to prevent vertical snapping during reposition
                targetPlayerPosition.y = transform.position.y;

                // Dynamic reposition speed based on player velocity
                float playerSpeed = _rb.linearVelocity.magnitude;
                float dynamicRepositionSpeed = Mathf.Max(pullRepositionSpeed, playerSpeed * 2f);

                StartSmoothRepositioning(targetPlayerPosition, dynamicRepositionSpeed); // This sets isRepositioning = true

                movementDirection = Vector3.zero;
                pullConstraintsReset = false;
                _anim.SetBool("Pull", true); // Start animation
                _anim.SetBool("Idle", false);

                // DO NOT CALL StartPull(pullObstacle);
            }
        }
        else
        {
            // No obstacle found, but pull button is held. Stop.
            StopPull();
        }

    }


    /// <summary>
    /// Runs every FixedUpdate while player is kinematic and pulling.
    /// Moves the obstacle and "attaches" the player to it to prevent jitter.
    /// </summary>
    private void HandlePull_Continue()
    {
        if (pullObstacle == null)
        {
            StopPull();
            return;
        }

        // **** NEW VALIDATION CHECK ****
        // Check if the obstacle has fallen too far below the player
        float yDifference = transform.position.y - pullObstacle.transform.position.y;
        if (yDifference > 0.8f) // Assuming player height is ~1.0, 0.8f seems like a safe threshold
        {
            Debug.Log("Obstacle fell too far below player. Stopping pull.");
            StopPull();
            return;
        }
        // ****************************


        // --- 1. Move the Obstacle ---
        pullDirection = -playerController._movement.GetFacingDirection();
        if (pullObstacle.MoveOverride)
        {
            _anim.SetBool("Pull", true); // Ensure anim is playing
        }
        else
        {
            // StartPull is now modified to ONLY move the obstacle
            StartPull(pullObstacle);
        }

        // --- 2. "Attach" Player to Obstacle ---
        Vector3 normalizedPullDirection = pullDirection.normalized;
        float obstacleHalfSize = 0.5f;
        float playerHalfSize = ((CapsuleCollider)playerCollider).radius;
        float offsetDistance = obstacleHalfSize + playerHalfSize + pullDistance;
        Vector3 offset = normalizedPullDirection * offsetDistance;

        Vector3 targetPlayerPosition = pullObstacle.transform.position + offset;

        // --- 3. Handle Falling (Replaces ControlPlayerFallWhilePulling) ---
        if (pullObstacle.isFalling && !playerMovement.IsGrounded)
        {
            targetPlayerPosition.y = pullObstacle.transform.position.y;
        }
        else
        {
            // Maintain player's current Y if they are grounded or obstacle is grounded
            targetPlayerPosition.y = transform.position.y;
        }

        _rb.MovePosition(targetPlayerPosition);
    }

    /// <summary>
    /// This function now ONLY moves the obstacle. The player is moved in HandlePull_Continue.
    /// </summary>
    void StartPull(Obstacle obs)
    {
        if (!_anim.GetBool("Pull"))
            _anim.SetBool("Pull", true);
        _anim.SetBool("Idle", false);
        if (playerController._movement.obstacleBehind) return;
        obs.isBeingPulled = true;
        obs.wasRecentlyPushed = true;
        if (obs != null && !playerController.AI) obs.currentlyUsedPlayerConrtoller = playerController;
        float speed = player.PushAndPullSpeed(obs.Weight);
        obs.PullObstacle(pullDirection, speed, playerController._movement.obstacleBehind);

        // --- REMOVED THIS LINE ---
        // _rb.MovePosition(_rb.transform.position + pullDirection * speed * Time.fixedDeltaTime);
    }
    void StopPull()
    {
        if (!playerController.AI)
            AudioManager.Instance.StopObstacleSound_Move();

        playerMovement.ResetPullConstraints(pullObstacle); // This resets kinematic state

        if (pullObstacle != null)
        {
            pullObstacle.isBeingPulled = false;
            pullObstacle.currentlyUsedPlayerConrtoller = null;
        }

        pullObstacle = null;
        playerController.StopPull();

        isRepositioning = false; // Reset repositioning flag
        repositionProgress = 0f; // Reset repositioning progress
    }

    [SerializeField] private float manualPushDistance = 0.1f; // Manually control how far the player should stand from the obstacle

}
