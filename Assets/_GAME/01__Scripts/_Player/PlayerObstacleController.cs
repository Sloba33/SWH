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

    private void Start()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        player = GetComponent<Player>();
        pushSpeed = player.StartingMoveSpeed / 2;
        playerMovement = GetComponent<PlayerMovement>();
    }
    private bool pullConstraintsReset;
    private void FixedUpdate()
    {

        movementDirection = playerController._movement.CurrentMoveDirection;
        if (!playerController.isPushing && previousPushObstacle != null)
        {
            previousPushObstacle.ResetObstacle();
            previousPushObstacle = null;
        }
        HandlePush();
        playerController._movement.SetPullConstraints(pullObstacle);
        if (playerController.isPulling && !playerController.pullButtonReleased)
            HandlePull();
        // Debug.Log(" Constraints Reset : " + pullConstraintsReset + " - Pull button held : " + playerController._pullButtonHeld + " Pull button released: " + playerController._pullButtonReleased);
        if (!pullConstraintsReset && !playerController.pullButtonHeld && playerController.pullButtonReleased)
        {
            Debug.Log("Pull stopped");
            pullConstraintsReset = true;
            StopPull();
        }

    }
    // In PlayerObstacleController.cs

    // Add a field to track the last time we landed
    private float lastLandTime = 0f;
    private const float PUSH_RE_EVAL_DELAY_AFTER_LAND = 0.2f; // Time to delay full push re-evaluation after landing

    public void HandlePush()
    {
        Debug.Log("Pushing");
        if (playerController.isPulling) return;

        // Capture the current timestamp if player just landed
        if (playerMovement.justLanded && lastLandTime == 0f) // Only set on the very first frame of justLanded
        {
            lastLandTime = Time.time;
        }
        // If we are past the grace period, reset lastLandTime
        if (lastLandTime != 0f && (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
        {
            lastLandTime = 0f;
        }


        // Condition 1: If player just jumped out of a push and is still ascending
        if (playerMovement.justJumpedOutOfPush && _rb.linearVelocity.y > 0.1f)
        {
            // Don't try to re-push while still jumping upwards
            return;
        }

        // Determine if we should *attempt* to push based on wall contact and movement
        bool canPotentiallyPush = playerController._movement.IsAgainstWall && movementDirection != Vector3.zero;

        // Condition 2: If we are not currently pushing, and cannot potentially push, then stop.
        // However, add a check for the landing grace period.
        if (!playerController.isPushing && !canPotentiallyPush)
        {
            // Only stop if we are definitely not against a wall OR not moving,
            // AND we are NOT in the grace period after landing where we might re-engage.
            if (lastLandTime == 0f || (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
            {
                // If not within the landing grace period, proceed to stop push
                previousPushDirection = Vector3.zero;
                if (pushObstacle != null) pushObstacle.ResetObstacle();
                StopPush();
                return;
            }
            else
            {
                // Within landing grace period, do not prematurely stop pushing.
                // Allow the next frame to potentially re-establish push conditions.
                return;
            }
        }


        // Main logic for finding and engaging with the obstacle
        // This block is entered if:
        // 1. We are currently pushing (playerController.isPushing == true) OR
        // 2. We can potentially push (canPotentiallyPush == true) AND
        //    we are NOT in the process of stopping prematurely due to landing issues (handled above)
        if (canPotentiallyPush)
        {
            pushObstacle = playerController.FindObstacle();
            if (pushObstacle == null)
            {
                // If no obstacle found, stop pushing. This applies even during grace period if no obstacle is found.
                previousPushDirection = Vector3.zero;
                StopPush();
                return;
            }

            pushObstacle.SphereFlags();
            if (!pushObstacle.isPushable)
            {
                // If obstacle is not pushable, stop pushing.
                previousPushDirection = Vector3.zero;
                pushObstacle?.ResetObstacle();
                StopPush();
                return;
            }
            else
            {
                // Conditions met to set isPushing to true
                playerController.isPushing = true;
            }

            // --- Rest of your existing HandlePush logic follows here ---
            // This part determines if the Push() method is actually called.
            // It should remain largely the same, as the goal is to prevent the flickering
            // of playerController.isPushing, not to change the push mechanics themselves.

            if (previousPushObstacle != pushObstacle)
            {
                if (previousPushObstacle != null) previousPushObstacle.ResetObstacle();
                previousPushObstacle = pushObstacle;
            }
            pushObstacle.SphereFlags();
            bool Moveable = pushObstacle.CheckObstaclesAround(movementDirection);
            Debug.Log("Moveable " + Moveable + " pushObstacle : " + pushObstacle + " Can push : " + playerController._movement.CanPush);

            if (playerController._movement.hasRecentlyFallen)
            {
                diff = Mathf.Round(playerController._movement.fallHeight) - pushObstacle.transform.position.y > 0;
            }
            else diff = true;

            if (pushObstacle != null && movementDirection != Vector3.zero && playerController._movement.CanPush && Moveable && diff && !pushObstacle.pushabilityDelayed)
            {
                Vector3 direction = pushObstacle.transform.position - transform.position;
                pushObstacle.playerController = playerController;
                direction.Normalize();
                if (previousPushObstacle == null || previousPushObstacle != pushObstacle)
                {
                    pushObstacle.ResetObstacle();
                    previousPushObstacle = pushObstacle;
                }
                Vector3 cubeSideCenter = pushObstacle.transform.position - pushObstacle.transform.GetComponent<Collider>().bounds.extents.magnitude * direction;
                transform.position = cubeSideCenter;

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

                if (currentMoveDirection != previousMoveDirection && pushObstacle != null && playerController._movement.CanPush && pushObstacle.Movable(movementDirection))
                {
                    pushDirectionChanged = true;
                    if (pushDirectionChanged)
                    {
                        pushDirectionChanged = false;
                        previousMoveDirection = currentMoveDirection;
                        return;
                    }
                }

                if (pushObstacle != null && Moveable && currentMoveDirection == previousMoveDirection && playerController._movement.CanPush)
                {
                    Push();
                }
                else
                {
                    bool pd = previousPushDirection == movementDirection;
                }

                previousMoveDirection = currentMoveDirection;
            }
            else if (!pushObstacle.grounded && pushObstacle.isFalling && !playerController._movement.IsGrounded && Moveable)
            {
                Push();
            }
            else if (!diff && Moveable) { Push(); }
            else
            {
                // If the above conditions for actually performing a push are not met,
                // then ensure isPushing is false and reset obstacle.
                pushObstacle?.ResetObstacle();
                StopPush();
            }
        }
        else // This is the "else" for the main "if (canPotentiallyPush)"
        {
            // If we cannot potentially push at all (not against wall or no movement),
            // then explicitly stop pushing, unless we are in the landing grace period.
            if (lastLandTime == 0f || (Time.time - lastLandTime) > PUSH_RE_EVAL_DELAY_AFTER_LAND)
            {
                if (pushObstacle != null)
                {
                    pushObstacle.ResetObstacle();
                }
                StopPush();
            }
        }
    }
    public bool pushDirectionChanged = false;
    void Push()
    {
        if (pushDirectionChanged)
        {
            pushDirectionChanged = false;
            return;
        }

        playerController.isPushing = true;
        pushObstacle.isBeingPushed = true;
        pushObstacle.wasRecentlyPushed = true;

        // Don't let individual movement systems handle this
        // Instead, use a coordinated movement approach
        StartCoroutine(CoordinatedPushMovement());

        _anim.SetBool("Push", true);
        _anim.SetBool("Idle", false);
        AudioManager.Instance.PlayObstacleSound_Move(pushObstacle.obstacleAudioType, transform.position);
    }
    private IEnumerator CoordinatedPushMovement()
    {
        while (playerController.isPushing && pushObstacle != null)
        {
            Vector3 moveDirection = movementDirection.normalized;
            float speed = player.PushAndPullSpeed(pushObstacle.Weight);

            if (moveDirection != Vector3.zero)
            {
                // Move both together in FixedUpdate timing
                yield return new WaitForFixedUpdate();

                Vector3 deltaMovement = moveDirection * speed * Time.fixedDeltaTime;

                // Move player
                _rb.MovePosition(transform.position + deltaMovement);

                // Move obstacle (maintaining relative position)
                if (pushObstacle != null)
                    pushObstacle._rb.MovePosition(pushObstacle.transform.position + deltaMovement);
            }
            else
            {
                yield return null;
            }
        }
    }
    public void StopPush()
    {
        if (!playerController.AI) AudioManager.Instance.StopObstacleSound_Move();
        if (pushObstacle != null && pushObstacle.isBeingPulled) return;
        _anim.SetBool("Push", false);
        if (pushObstacle != null && !pushObstacle._rb.isKinematic) pushObstacle._rb.linearVelocity = Vector3.zero; // Stop the movement;
        if (pushObstacle != null) pushObstacle.ResetObstacle();
        if (pushObstacle != null) pushObstacle.currentlyUsedPlayerConrtoller = null;
        pushObstacle = null;
        previousPushObstacle = null;
        previousPushDirection = Vector3.zero;
        previousMoveDirection = Vector3.zero; // Reset previousMoveDirection
        playerController.isPushing = false;
        if (!player.blackHoleDebuff) player.MoveSpeed = player.StartingMoveSpeed;
        // Debug.Log("Stopping push");'
        pushDirectionChanged = false;
        return;
    }


    public float delayTimer;
    private bool started, ended;

    public void HandlePull()
    {
        // playerController.isPushing = false;
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
            // Debug.Log("PULL - Obstacle found");
            pullObstacle = hit.transform.GetComponent<Obstacle>();

            if (pullObstacle != null && pullObstacle.grounded && pullObstacle.isPullable)
            {
                pullObstacle.SphereFlags();
                if (playerController.isPulling)
                {
                    playerController._movement.CanMove = false;
                    pullObstacle.playerController = playerController;
                    pullDirection = -playerController._movement.GetFacingDirection();
                    if (!playerController._movement.IsGrounded)
                    {
                        // Debug.Log("PULL - player repositioned");
                        float newHeight = pullObstacle.transform.position.y;
                        Vector3 setHeight = new(transform.position.x, newHeight, transform.position.z);
                        transform.position = setHeight;
                    }
                    Vector3 testSide = pullObstacle.transform.position * pullObstacle.transform.GetComponent<Collider>().bounds.extents.magnitude;
                    Vector3 cubeSideCenter = pullObstacle.transform.position - pullObstacle.transform.GetComponent<Collider>().bounds.extents.magnitude * -pullDirection;
                    transform.position = cubeSideCenter;

                    movementDirection = Vector3.zero;
                    pullConstraintsReset = false;
                    // Debug.Log("PULL - Starting regular pull after repositioning");
                    if (pullObstacle.MoveOverride) _anim.SetBool("Pull", true);
                    else
                        StartPull(pullObstacle);
                }
            }
        }
        else
        {
            // Debug.Log("PULL - No target found");
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
            // Debug.Log("Speed : " + speed);
            // obs.PullObstacle(pullDirection, speed, _rb, playerController.obstacleBehind);
            obs.PullObstacle(pullDirection, speed, playerController._movement.obstacleBehind);
            _rb.MovePosition(_rb.transform.position + pullDirection * speed * Time.fixedDeltaTime);
        }

    }
    void StopPull()
    {
        if (!playerController.AI) AudioManager.Instance.StopObstacleSound_Move();
        playerController._movement.ResetPullConstraints(pullObstacle);
        if (pullObstacle != null) pullObstacle.isBeingPulled = false;
        // pullObstacle.playerController = null;
        if (pullObstacle != null) pullObstacle.currentlyUsedPlayerConrtoller = null;
        pullObstacle = null;
        playerMovement.CanMove = true;
        playerController.StopPull();
        // Debug.Log("PULL - Stopping pull");
        return;
    }
}