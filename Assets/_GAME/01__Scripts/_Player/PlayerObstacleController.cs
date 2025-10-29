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
        }

        if (!playerController.isPushing && previousPushObstacle != null)
        {
            previousPushObstacle.ResetObstacle();
            previousPushObstacle = null;
        }
        HandlePush();
        playerController._movement.SetPullConstraints(pullObstacle);
        if (playerController.isPulling && !playerController.pullButtonReleased)
        {
            ControlPlayerFallWhilePulling();
            HandlePull();
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
        AudioManager.Instance.PlayObstacleSound_Move(pushObstacle.obstacleAudioType, transform.position);
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
            Debug.LogWarning("[StopPush] Prevented push stop — lockMidAirPush active");
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
        else if (Physics.Raycast(ray, out RaycastHit hit, 0.5f, playerController._movement._obstacleMask))
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

                    // Dynamic reposition speed based on player velocity
                    float playerSpeed = _rb.linearVelocity.magnitude;
                    float dynamicRepositionSpeed = Mathf.Max(pullRepositionSpeed, playerSpeed * 2f);

                    StartSmoothRepositioning(targetPlayerPosition, dynamicRepositionSpeed);

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
    void StopPull()
    {
        if (!playerController.AI)
            AudioManager.Instance.StopObstacleSound_Move();

        playerMovement.ResetPullConstraints(pullObstacle);

        if (pullObstacle != null)
        {
            pullObstacle.isBeingPulled = false;
            pullObstacle.currentlyUsedPlayerConrtoller = null;
        }

        pullObstacle = null;
        playerController.StopPull();
    }

    [SerializeField] private float manualPushDistance = 0.1f; // Manually control how far the player should stand from the obstacle

}