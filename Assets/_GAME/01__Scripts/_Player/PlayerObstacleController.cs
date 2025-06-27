using System.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using UnityEngine;


public class PlayerObstacleController : MonoBehaviour
{
    // separate obstacle shit a bit more
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

    private bool pullConstraintsReset;
    private void FixedUpdate()
    {
        movementDirection = playerController._movement.CurrentMoveDirection;

        // Handle smooth repositioning
        if (isRepositioning)
        {
            HandleSmoothRepositioning();
        }

        if (!playerController.isPushing && previousPushObstacle != null)
        {
            previousPushObstacle.ResetObstacle();
            previousPushObstacle = null;
        }
        HandlePush();
        playerController._movement.SetPullConstraints(pullObstacle);
        if (playerController.isPulling && !playerController.pullButtonReleased)
            HandlePull();
        if (!pullConstraintsReset && !playerController.pullButtonHeld && playerController.pullButtonReleased)
        {
            Debug.Log("Pull stopped");
            pullConstraintsReset = true;
            StopPull();

        }
    }

    private void HandleSmoothRepositioning()
    {
        repositionProgress += repositionSpeed * Time.fixedDeltaTime;

        if (repositionProgress >= 1f)
        {
            // Repositioning complete
            transform.position = targetPosition;
            isRepositioning = false;
            repositionProgress = 0f;
        }
        else
        {
            // Smoothly interpolate between start and target position
            transform.position = Vector3.Lerp(startPosition, targetPosition, repositionProgress);
        }
    }

    private void StartSmoothRepositioning(Vector3 newTargetPosition)
    {
        if (!isRepositioning || Vector3.Distance(targetPosition, newTargetPosition) > 0.1f)
        {
            startPosition = transform.position;
            targetPosition = newTargetPosition;
            isRepositioning = true;
            repositionProgress = 0f;
            Debug.Log("Starting smooth repositioning to: " + targetPosition);
        }
    }

    private float lastLandTime = 0f;
    private const float PUSH_RE_EVAL_DELAY_AFTER_LAND = 0.2f; // Time to delay full push re-evaluation after landing
    public void HandlePush()
    {
        if (playerController.isPulling)
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

        if (!playerController.isPushing && !canPotentiallyPush)
        {
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

            if (foundObstacle != null)
            {
                if (pushObstacle == null || pushObstacle != foundObstacle)
                {
                    if (pushObstacle != null) pushObstacle.ResetObstacle();
                    pushObstacle = foundObstacle;
                }
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

            if (playerController._movement.hasRecentlyFallen)
                diff = Mathf.Round(playerController._movement.fallHeight) - pushObstacle.transform.position.y >= 0;
            else
                diff = true;

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

                Vector3 currentPosition = _rb.position;
                Vector3 positionDelta = targetPlayerPosition - currentPosition;
                _rb.MovePosition(targetPlayerPosition);

                if (positionDelta.sqrMagnitude < 0.005f * 0.005f)
                    _rb.linearVelocity = Vector3.zero;

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

                _rb.MovePosition(Vector3.Lerp(_rb.position, targetPlayerPosition, pushCenteringSmoothFactor));

                Push();
            }
            else if (!diff && Moveable)
            {
                Push();
            }
            else
            {
                pushObstacle?.ResetObstacle();
                StopPush();
            }
        }
        else
        {
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
    }
    /// <summary>
    /// old push logic, now with more checks and conditions
    /// </summary>
    // public void HandlePush()
    // {
    //     // Debug.Log("--- HandlePush() Start ---");
    //     // NEW DEBUG: Log current pushObstacle at the very start of the function
    //     // Debug.Log($"[DEBUG] Start of HandlePush. Current pushObstacle: {(pushObstacle != null ? pushObstacle.name : "NULL")}");

    //     // Debug.Log($"Current Player Pulling State: {playerController.isPulling}");
    //     if (playerController.isPulling)
    //     {
    //         // Debug.Log("HandlePush: Player is currently pulling, returning early.");
    //         return;
    //     }
    //     // Debug.Log("Pushing");

    //     // Capture the current timestamp if player just landed
    //     // Debug.Log("Player just landed: " + playerMovement.justLanded);
    //     // Debug.Log("Last land time: " + lastLandTime);
    //     if (playerMovement.justLanded && lastLandTime == 0f) // Only set on the very first frame of justLanded
    //     {
    //         lastLandTime = Time.time;
    //         // Debug.Log($"HandlePush: lastLandTime set to {lastLandTime} (just landed).");
    //     }
    //     // If we are past the grace period, reset lastLandTime
    //     if (lastLandTime != 0f && (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
    //     {
    //         lastLandTime = 0f;
    //         // Debug.Log("HandlePush: Resetting lastLandTime after grace period.");
    //     }
    //     // Debug.Log($"HandlePush: Current lastLandTime: {lastLandTime}, Time since last land: {(Time.time - lastLandTime):F3}, PUSH_RE_EVAL_DELAY_AFTER_LAND: {PUSH_RE_EVAL_DELAY_AFTER_LAND}");


    //     // Debug.Log("Just jumped out of push: " + playerMovement.justJumpedOutOfPush);
    //     // Debug.Log($"Rigidbody linearVelocity.y: {_rb.linearVelocity.y}");
    //     // Condition 1: If player just jumped out of a push and is still ascending
    //     if (playerMovement.justJumpedOutOfPush && _rb.linearVelocity.y > 0.1f)
    //     {
    //         // Debug.Log("HandlePush: Player just jumped out of push and is still ascending. Stopping push.");
    //         // NEW DEBUG: Log before resetting
    //         // Debug.Log($"[DEBUG] Resetting obstacle in 'jumped out' block: {(pushObstacle != null ? pushObstacle.name : "NULL")}");
    //         if (pushObstacle != null) pushObstacle.ResetObstacle();
    //         StopPush();
    //         return;
    //     }

    //     // Determine if we should *attempt* to push based on wall contact and movement
    //     Debug.Log("is against wall: " + playerController._movement.IsAgainstWall + " and movement direction: " + (movementDirection!= Vector3.zero));
    //     bool canPotentiallyPush = playerController._movement.IsAgainstWall && movementDirection != Vector3.zero;
    //     // Debug.Log($"HandlePush: IsAgainstWall: {playerController._movement.IsAgainstWall}, MovementDirection: {movementDirection}");
    //     // Debug.Log($"HandlePush: Can potentially push: {canPotentiallyPush}");
    //     // Condition 2: If we are not currently pushing, and cannot potentially push, then stop.
    //     // However, add a check for the landing grace period.
    //     // Debug.Log($"HandlePush: Current playerController.isPushing: {playerController.isPushing}");
    //     if (!playerController.isPushing && !canPotentiallyPush)
    //     {
    //         // Debug.Log("HandlePush: Player not pushing AND cannot potentially push.");
    //         // Only stop if we are definitely not against a wall OR not moving,
    //         // AND we are NOT in the grace period after landing where we might re-engage.
    //         if (lastLandTime == 0f || (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
    //         {
    //             // Debug.Log("HandlePush: Outside landing grace period. Stopping push because not against wall/not moving.");
    //             previousPushDirection = Vector3.zero;
    //             // NEW DEBUG: Log before resetting
    //             // Debug.Log($"[DEBUG] Resetting obstacle in '!isPushing && !canPotentiallyPush (outside grace)': {(pushObstacle != null ? pushObstacle.name : "NULL")}");
    //             if (pushObstacle != null) pushObstacle.ResetObstacle();
    //             StopPush();
    //             // Debug.Log("HandlePush: Stopped push and returning.");
    //             return;
    //         }
    //         else
    //         {
    //             // MODIFIED: Within landing grace period, do not prematurely stop pushing.
    //             // However, if we cannot potentially push, ensure obstacle and player push states are reset.
    //             // Debug.Log("HandlePush: Within landing grace period. Not starting push, but ensuring obstacle/player push state is reset.");
    //             // NEW DEBUG: Log before resetting
    //             // Debug.Log($"[DEBUG] Resetting obstacle in '!isPushing && !canPotentiallyPush (inside grace)': {(pushObstacle != null ? pushObstacle.name : "NULL")}");
    //             if (pushObstacle != null)
    //             {
    //                 pushObstacle.ResetObstacle();
    //             }
    //             StopPush();
    //             return;
    //         }
    //     }
    //     // Debug.Log("HandlePush: Passed initial early exit conditions.");

    //     // Main logic for finding and engaging with the obstacle
    //     if (canPotentiallyPush)
    //     {
    //         // Debug.Log("HandlePush: Attempting to push obstacle as 'canPotentiallyPush' is true.");

    //         Obstacle foundObstacle = playerController.FindObstacle(); // Call FindObstacle
    //                                                                   // NEW DEBUG: Log result of FindObstacle immediately
    //                                                                   // Debug.Log($"[DEBUG] playerController.FindObstacle() returned: {(foundObstacle != null ? foundObstacle.name : "NULL")}");

    //         // Only update pushObstacle if it's different or currently null
    //         if (foundObstacle != null)
    //         {
    //             if (pushObstacle == null || pushObstacle != foundObstacle)
    //             {
    //                 // Debug.Log($"[DEBUG] Changing pushObstacle from {(pushObstacle != null ? pushObstacle.name : "NULL")} to {foundObstacle.name}");
    //                 if (pushObstacle != null) // If we were previously pushing something, reset it before changing
    //                 {
    //                     pushObstacle.ResetObstacle();
    //                     // Debug.Log($"[DEBUG] Resetting previous pushObstacle: {pushObstacle.name}");
    //                 }
    //                 pushObstacle = foundObstacle;
    //             }
    //         }
    //         else if (pushObstacle != null && playerController.isPushing)
    //         {
    //             // If FindObstacle returns null but we were pushing, something went wrong, so stop pushing the current one.
    //             // Debug.Log($"[DEBUG] FindObstacle returned NULL, but player was pushing {pushObstacle.name}. Resetting and stopping push.");
    //             pushObstacle.ResetObstacle();
    //             StopPush();
    //             return;
    //         }
    //         else
    //         {
    //             // If FindObstacle is null and not currently pushing, ensure pushObstacle is null
    //             pushObstacle = null;
    //         }


    //         // Debug.Log($"HandlePush: Current pushObstacle after assignment/check: {(pushObstacle != null ? pushObstacle.name : "NULL")}");


    //         if (pushObstacle == null) // Re-check after potential reassignment
    //         {
    //             // If no obstacle found, stop pushing. This applies even during grace period if no obstacle is found.
    //             // Debug.Log("HandlePush: No obstacle found AFTER assignment. Stopping push and returning.");
    //             previousPushDirection = Vector3.zero;
    //             StopPush();
    //             return;
    //         }

    //         pushObstacle.SphereFlags();
    //         // Debug.Log($"HandlePush: Obstacle '{pushObstacle.name}' isPushable: {pushObstacle.isPushable}");
    //         if (!pushObstacle.isPushable)
    //         {
    //             // Debug.Log($"HandlePush: Obstacle '{pushObstacle.name}' is NOT pushable. Stopping push and returning.");
    //             previousPushDirection = Vector3.zero;
    //             // NEW DEBUG: Log before resetting
    //             // Debug.Log($"[DEBUG] Resetting obstacle in '!isPushable' block: {(pushObstacle != null ? pushObstacle.name : "NULL")}");
    //             pushObstacle?.ResetObstacle();
    //             StopPush();
    //             return;
    //         }
    //         else
    //         {
    //             // Conditions met to set isPushing to true
    //             playerController.isPushing = true;
    //             // Debug.Log("HandlePush: Conditions met. Setting playerController.isPushing = TRUE.");
    //         }

    //         if (previousPushObstacle != pushObstacle)
    //         {
    //             // Debug.Log($"HandlePush: Previous obstacle changed from {(previousPushObstacle != null ? previousPushObstacle.name : "NULL")} to {pushObstacle.name}.");
    //             // NEW DEBUG: Log before resetting
    //             // Debug.Log($"[DEBUG] Resetting previousPushObstacle in 'previousObstacle change' block: {(previousPushObstacle != null ? previousPushObstacle.name : "NULL")}");
    //             if (previousPushObstacle != null) previousPushObstacle.ResetObstacle();
    //             previousPushObstacle = pushObstacle;
    //         }
    //         pushObstacle.SphereFlags();
    //         bool Moveable = pushObstacle.CheckObstaclesAround(movementDirection);
    //         Debug.Log($"HandlePush: Obstacle Moveable (from CheckObstaclesAround): {Moveable}");

    //         // Original logic for diff - now using >=
    //         if (playerController._movement.hasRecentlyFallen)
    //         {
    //             diff = Mathf.Round(playerController._movement.fallHeight) - pushObstacle.transform.position.y >= 0; // Modified to >=
    //             // Debug.Log($"DEBUG: Player fallHeight (rounded): {Mathf.Round(playerController._movement.fallHeight)}"); // Debug added
    //             // Debug.Log($"DEBUG: PushObstacle Y position: {pushObstacle.transform.position.y}"); // Debug added
    //             // Debug.Log($"DEBUG: Difference: {Mathf.Round(playerController._movement.fallHeight) - pushObstacle.transform.position.y}"); // Debug added
    //             // Debug.Log($"HandlePush: hasRecentlyFallen is TRUE. FallHeight: {playerController._movement.fallHeight}, Obstacle Y: {pushObstacle.transform.position.y}, Calculated Diff: {diff}");
    //         }
    //         else
    //         {
    //             diff = true;
    //             // Debug.Log("HandlePush: hasRecentlyFallen is FALSE. Diff is TRUE.");
    //         }
    //         // Debug.Log($"HandlePush: pushObstacle.pushabilityDelayed: {pushObstacle.pushabilityDelayed}");


    //         // --- Main Centering and Push Condition Check ---
    //         Debug.Log("--- Centering/Pushing Main Condition Breakdown ---");
    //         Debug.Log($"Condition 1 (pushObstacle != null): {pushObstacle != null}");
    //         Debug.Log($"Condition 2 (movementDirection != Vector3.zero): {movementDirection != Vector3.zero}");
    //         Debug.Log($"Condition 3 (playerController._movement.CanPush): {playerController._movement.CanPush}");
    //         Debug.Log($"Condition 4 (Moveable): {Moveable}");
    //         Debug.Log($"Condition 5 (diff): {diff}");
    //         Debug.Log($"Condition 6 (!pushObstacle.pushabilityDelayed): {!pushObstacle.pushabilityDelayed}");
    //         Debug.Log($"Combined main centering condition result: {(pushObstacle != null && movementDirection != Vector3.zero && playerController._movement.CanPush && Moveable && diff && !pushObstacle.pushabilityDelayed)}");
    //         Debug.Log("--- End Centering/Pushing Main Condition Breakdown ---");


    //         if (pushObstacle != null && movementDirection != Vector3.zero && playerController._movement.CanPush && Moveable && diff && !pushObstacle.pushabilityDelayed)
    //         {
    //             // Debug.Log("HandlePush: ALL main centering conditions met. Proceeding with centering logic.");
    //             // Optimized Snapping Logic for 1x1x1 Cubes
    //             if (playerCollider == null)
    //             {
    //                 // Debug.LogError("HandlePush: Player collider not assigned. Cannot snap. Stopping push.");
    //                 StopPush();
    //                 return;
    //             }

    //             Vector3 normalizedMovementDirection = movementDirection.normalized;

    //             float playerHalfSize = ((CapsuleCollider)playerCollider).radius;
    //             float obstacleHalfSize = 0.5f; // Assuming 1x1x1 cube obstacles
    //             float combinedSizeAndOffset = obstacleHalfSize + playerHalfSize + manualPushDistance;

    //             // --- MODIFIED LOGIC FOR CENTERING (SAME AS BEFORE) ---
    //             Vector3 targetPlayerPosition = pushObstacle.transform.position;

    //             if (Mathf.Abs(normalizedMovementDirection.x) > 0.01f) // Moving along X-axis
    //             {
    //                 targetPlayerPosition.x = pushObstacle.transform.position.x - (normalizedMovementDirection.x * combinedSizeAndOffset);
    //                 targetPlayerPosition.z = pushObstacle.transform.position.z;
    //             }
    //             else if (Mathf.Abs(normalizedMovementDirection.z) > 0.01f) // Moving along Z-axis
    //             {
    //                 targetPlayerPosition.z = pushObstacle.transform.position.z - (normalizedMovementDirection.z * combinedSizeAndOffset);
    //                 targetPlayerPosition.x = pushObstacle.transform.position.x;
    //             }
    //             targetPlayerPosition.y = pushObstacle.transform.position.y;
    //             // --- END MODIFIED LOGIC ---

    //             // --- NEW: Using Rigidbody velocity with Lerp for smoother centering ---
    //             Vector3 currentPosition = _rb.position;
    //             Vector3 positionDelta = targetPlayerPosition - currentPosition;

    //             // Calculate the velocity needed to reach the target within one FixedUpdate frame
    //             Vector3 desiredVelocity = positionDelta / Time.fixedDeltaTime;
    //             //  _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVelocity, pushCenteringSmoothFactor);


    //             _rb.MovePosition(targetPlayerPosition);

    //             // Optional: If player is very close to the target, set velocity to zero to prevent micro-adjustments/jitter
    //             if (positionDelta.sqrMagnitude < 0.005f * 0.005f) // Using 0.005f (5mm) as an example threshold
    //             {
    //                 _rb.linearVelocity = Vector3.zero;
    //                 // Debug.Log("HandlePush: Player very close to target position, setting linear velocity to zero.");
    //             }
    //             // Debug.Log($"HandlePush: Centering. Target Position: {targetPlayerPosition}, Current Position: {_rb.position}, Position Delta: {positionDelta}, Desired Velocity: {desiredVelocity}, Applied Linear Velocity: {_rb.linearVelocity}");

    //             currentMoveDirection = movementDirection;

    //             if (previousMoveDirection == Vector3.zero && currentMoveDirection != Vector3.zero)
    //             {
    //                 // Debug.Log("HandlePush: Push direction change (from zero to non-zero).");
    //                 pushDirectionChanged = true;
    //                 if (pushDirectionChanged) // This check is redundant as it's just set to true
    //                 {
    //                     pushDirectionChanged = false;
    //                     previousMoveDirection = currentMoveDirection;
    //                     // StopPush();
    //                     // Debug.Log("HandlePush: Processed push direction change. Returning (as per original logic).");
    //                     return;
    //                 }
    //             }

    //             if (currentMoveDirection != previousMoveDirection && pushObstacle != null && playerController._movement.CanPush && pushObstacle.Movable(movementDirection))
    //             {
    //                 // Debug.Log("HandlePush: Push direction changed (between non-zero directions).");
    //                 pushDirectionChanged = true;
    //                 if (pushDirectionChanged) // This check is redundant as it's just set to true
    //                 {
    //                     pushDirectionChanged = false;
    //                     previousMoveDirection = currentMoveDirection;
    //                     // Debug.Log("HandlePush: Processed push direction change. Returning (as per original logic).");
    //                     return;
    //                 }
    //             }
    //             previousPushDirection = movementDirection;

    //             // Only start pushing if we're close enough to the target position or already repositioned
    //             if (pushObstacle != null && Moveable && playerController._movement.CanPush)
    //             {
    //                 // Debug.Log("HandlePush: Conditions met for calling Push() (obstacle movement).");
    //                 Push();
    //             }
    //             else
    //             {
    //                 bool pd = previousPushDirection == movementDirection; // This line seems to do nothing on its own
    //                 // Debug.Log("HandlePush: Conditions NOT met for calling Push() (obstacle movement).");
    //             }

    //             previousMoveDirection = currentMoveDirection;
    //         }
    //         else if (!pushObstacle.grounded && pushObstacle.isFalling && !playerController._movement.IsGrounded && Moveable)
    //         {
    //             Debug.Log("HandlePush: Else if branch (obstacle falling, player not grounded). Calling Push().");
    //             Push();
    //         }
    //         else if (!diff && Moveable)
    //         {
    //             Debug.Log("HandlePush: Else if branch (!diff && Moveable). Calling Push().");
    //             Push();
    //         }
    //         else
    //         {
    //             Debug.Log("HandlePush: Final else branch within canPotentiallyPush. Stopping push.");
    //             // NEW DEBUG: Log before resetting
    //             // Debug.Log($"[DEBUG] Resetting obstacle in 'final else branch (canPotentiallyPush)': {(pushObstacle != null ? pushObstacle.name : "NULL")}");
    //             pushObstacle?.ResetObstacle();
    //             StopPush();
    //         }
    //     }
    //     else // This is the "else" for the main "if (canPotentiallyPush)"
    //     {
    //         Debug.Log("HandlePush: 'canPotentiallyPush' is FALSE.");
    //         // If we cannot potentially push at all (not against wall or no movement),
    //         // then explicitly stop pushing, unless we are in the landing grace period.
    //         if (lastLandTime == 0f || (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
    //         {
    //             // Debug.Log("HandlePush: 'canPotentiallyPush' is false and outside grace period. Stopping push.");
    //             // NEW DEBUG: Log before resetting
    //             // Debug.Log($"[DEBUG] Resetting obstacle in '!canPotentiallyPush (outside grace)': {(pushObstacle != null ? pushObstacle.name : "NULL")}");
    //             if (pushObstacle != null)
    //             {
    //                 pushObstacle.ResetObstacle();
    //             }
    //             StopPush();
    //         }
    //         else
    //         {
    //             // MODIFIED: Ensure obstacle is reset and player push state is stopped, even within grace period, if canPotentiallyPush is false.
    //             // Debug.Log("HandlePush: 'canPotentiallyPush' is false but within landing grace period. Ensuring obstacle/player push state is reset.");
    //             // NEW DEBUG: Log before resetting
    //             // Debug.Log($"[DEBUG] Resetting obstacle in '!canPotentiallyPush (inside grace)': {(pushObstacle != null ? pushObstacle.name : "NULL")}");
    //             if (pushObstacle != null)
    //             {
    //                 pushObstacle.ResetObstacle();
    //             }
    //             StopPush();
    //             return;
    //         }
    //     }
    //     // Debug.Log("--- HandlePush() End ---");
    // }
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

    StartCoroutine(CoordinatedPushMovement());

    _anim.SetBool("Push", true);
    _anim.SetBool("Idle", false);
    AudioManager.Instance.PlayObstacleSound_Move(pushObstacle.obstacleAudioType, transform.position);
}

    private IEnumerator CoordinatedPushMovement()
    {
        while (playerController.isPushing)
        {
            if (pushObstacle == null)
            {
                StopPush();
                yield break;
            }

            Vector3 moveDirection = movementDirection.normalized;
            float speed = player.PushAndPullSpeed(pushObstacle.Weight);

            if (moveDirection != Vector3.zero)
            {
                yield return new WaitForFixedUpdate();

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

        StopPush(); // clean up if loop exits naturally
    }

    public void StopPush()
    {
        if (!playerController.AI)
            AudioManager.Instance.StopObstacleSound_Move();

        if (pushObstacle != null && pushObstacle.isBeingPulled)
            return;

        _anim.SetBool("Push", false);

        if (pushObstacle != null)
        {
            // ✅ Reset the obstacle’s internal state (important!)
            pushObstacle.ResetObstacle();

            // Stop velocity if it's not kinematic
            if (!pushObstacle._rb.isKinematic)
                pushObstacle._rb.linearVelocity = Vector3.zero;

            // Detach player reference
            pushObstacle.currentlyUsedPlayerConrtoller = null;
        }
        pushObstacle = null;
        previousPushObstacle = null;
        previousPushDirection = Vector3.zero;
        previousMoveDirection = Vector3.zero; // Reset previousMoveDirection
        playerController.isPushing = false;

        pushDirectionChanged = false;

        // Stop repositioning when push stops
        isRepositioning = false;
        repositionProgress = 0f;

        return;
    }

    public float delayTimer;
    private bool started, ended;

    public void HandlePull()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hitCollectible, 0.5f, playerController._movement._collectibleMask))
        {
            if (hitCollectible.transform.TryGetComponent<CollectibleItem>(out var collectibleItem))
            {
                Debug.Log("Looted collectible");
                collectibleItem.Collect(playerController);
            }
        }
        else
        if (Physics.Raycast(ray, out RaycastHit hit, 0.5f, playerController._movement._obstacleMask))
        {
            pullObstacle = hit.transform.GetComponent<Obstacle>();

            if (pullObstacle != null && pullObstacle.grounded && pullObstacle.isPullable)
            {
                pullObstacle.SphereFlags();
                if (playerController.isPulling)
                {
                    playerController._movement.CanMove = false;
                    pullObstacle.playerController = playerController;
                    pullDirection = -playerController._movement.GetFacingDirection();

                    // Optimized Snapping Logic for 1x1x1 Cubes (PULL)
                    if (playerCollider == null)
                    {
                        Debug.LogError("Player collider not assigned. Cannot snap during pull.");
                        StopPull();
                        return;
                    }

                    Vector3 normalizedPullDirection = pullDirection.normalized;

                    float obstacleHalfSize = 0.5f;

                    float playerHalfSize = ((CapsuleCollider)playerCollider).radius;

                    // Target player position for pulling
                    Vector3 targetPlayerPosition = pullObstacle.transform.position + (normalizedPullDirection * (obstacleHalfSize + playerHalfSize + pullDistance));

                    // Snap player's Y to obstacle's Y if pulling from a different height, as in your old code
                    targetPlayerPosition.y = pullObstacle.transform.position.y;

                    // Use smooth repositioning for pull as well (optional - you can keep instant for pull if preferred)
                    StartSmoothRepositioning(targetPlayerPosition);

                    movementDirection = Vector3.zero;
                    pullConstraintsReset = false;
                    if (pullObstacle.MoveOverride) _anim.SetBool("Pull", true);
                    else
                        StartPull(pullObstacle);
                }
            }
        }
        else
        {
            StopPull();
        }

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
            _rb.MovePosition(_rb.transform.position + pullDirection * speed * Time.fixedDeltaTime);
        }

    }
    void StopPull()
    {
        if (!playerController.AI) AudioManager.Instance.StopObstacleSound_Move();
        playerController._movement.ResetPullConstraints(pullObstacle);
        if (pullObstacle != null) pullObstacle.isBeingPulled = false;
        if (pullObstacle != null) pullObstacle.currentlyUsedPlayerConrtoller = null;
        pullObstacle = null;
        playerMovement.CanMove = true;
        playerController.StopPull();
        return;
    }
    [SerializeField] private float manualPushDistance = 0.1f; // Manually control how far the player should stand from the obstacle

}