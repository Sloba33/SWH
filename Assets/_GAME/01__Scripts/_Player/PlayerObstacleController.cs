using Unity.Multiplayer.Tools.MetricTypes;
using UnityEngine;


public class PlayerObstacleController : MonoBehaviour
{
    // separate obstacle shit a bit more

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
    }
    private bool pullConstraintsReset;
    private void FixedUpdate()
    {

        movementDirection = playerController._dir;
        if (!playerController.isPushing && previousPushObstacle != null)
        {
            previousPushObstacle.ResetObstacle();
            previousPushObstacle = null;
        }
        HandlePush();
        playerController.SetPullConstraints(pullObstacle);
        if (playerController._pullButtonHeld && !playerController._pullButtonReleased)
            HandlePull();
        // Debug.Log(" Constraints Reset : " + pullConstraintsReset + " - Pull button held : " + playerController._pullButtonHeld + " Pull button released: " + playerController._pullButtonReleased);
        if (!pullConstraintsReset && !playerController._pullButtonHeld && playerController._pullButtonReleased)
        {
            Debug.Log("Pull stopped");
            pullConstraintsReset = true;
            StopPull();
        }

    }
    public void HandlePush()
    {
        if (playerController.isPulling) return;

        if (!playerController._isAgainstWall || movementDirection == Vector3.zero)
        {
            previousPushDirection = Vector3.zero;
            if (pushObstacle != null) pushObstacle.ResetObstacle();
            StopPush();
            return;
        }
        else if (playerController._isAgainstWall && movementDirection != Vector3.zero)
        {
            // Debug.Log("Pushing");
            pushObstacle = playerController.FindObstacle();
            pushObstacle.SphereFlags();
            if (!pushObstacle.isPushable)
            {
                // Debug.Log("Code 0");
                previousPushDirection = Vector3.zero;
                pushObstacle?.ResetObstacle();
                StopPush();
                return;
            }
            else
                playerController.isPushing = true;

            if (previousPushObstacle != pushObstacle)
            {
                // Debug.Log("Targetting different obstacle, resetting other");
                if (previousPushObstacle != null) previousPushObstacle.ResetObstacle();
                previousPushObstacle = pushObstacle;
            }
            // Debug.Log("Added obstacle");
            pushObstacle.SphereFlags();
            bool Moveable = pushObstacle.CheckObstaclesAround(movementDirection);
            // Debug.Log("fall height is : " + playerController.fallHeight);
            // diff = playerController.fallHeight - 0.1f > pushObstacle.transform.position.y
            if (playerController.hasRecentlyFallen)
            {
                diff = Mathf.Round(playerController.fallHeight) - pushObstacle.transform.position.y > 0;
                // Debug.Log("Testing fall - Fall height :" + playerController.fallHeight + " obstacle height " + pushObstacle.transform.position.y);
            }
            else diff = true;
            // Debug.Log("Should the obstacle be pushable :" + (playerController.fallHeight - 0.1f) + " obstacle height" + pushObstacle.transform.position.y);
            if (pushObstacle != null && movementDirection != Vector3.zero && playerController.canPush && Moveable && diff && !pushObstacle.pushabilityDelayed)
            {
                // Debug.Log("Should the obstacle be pushable INSIDE :" + diff);
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

                // Update currentMoveDirection
                currentMoveDirection = movementDirection;

                // Handle transition from standstill
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

                // Check for direction change and set flag
                if (currentMoveDirection != previousMoveDirection && pushObstacle != null && playerController.canPush && pushObstacle.Movable(movementDirection))
                {
                    pushDirectionChanged = true;
                    if (pushDirectionChanged)
                    {
                        pushDirectionChanged = false;
                        previousMoveDirection = currentMoveDirection;
                        return;
                    }
                }

                if (pushObstacle != null && playerController.canPush && Moveable && currentMoveDirection == previousMoveDirection)
                {
                    // Debug.Log("Code 1");
                    Push();
                }
                else
                {
                    bool pd = previousPushDirection == movementDirection;
                }

                // Update previousMoveDirection
                previousMoveDirection = currentMoveDirection;
            }
            else if (!pushObstacle.grounded && pushObstacle.isFalling && !playerController.IsGrounded)
            {
                // Debug.Log("Code 2");
                Push();
            }
            else if (!diff) { Debug.Log("Code 3 diff"); Push(); }
            else
            {
                // Debug.Log("CODE 3 " + "pushObstacle :" + pushObstacle + " Can push : " + playerController.canPush + " Moveable :" + Moveable + " diff :" + diff);
                pushObstacle?.ResetObstacle();
                StopPush();
            }
        }
        else
        {
            if (pushObstacle != null)
            {
                // Debug.Log("Code 4");
                pushObstacle.ResetObstacle();
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
     
        // Debug.Log("Starting Push");
        playerController.isPushing = true;
        pushObstacle.isBeingPushed = true;
        pushObstacle.wasRecentlyPushed = true;
        Vector3 dir = new(movementDirection.x, pushObstacle.transform.position.y, movementDirection.z);
     
        playerController._walkSpeed = pushSpeed;
        if (pushObstacle != null && !playerController.AI) pushObstacle.currentlyUsedPlayerConrtoller = playerController;
        if (pushObstacle.tile != null)
        {
            pushObstacle.isPositioned = false;
            pushObstacle.isHeightPositioned = false;
        }
        pushObstacle.PushObstacle(dir, player.PushAndPullSpeed(pushObstacle.Weight));
        _anim.SetBool("Push", true);
        _anim.SetBool("AFK", false);
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
        if (!player.blackHoleDebuff) playerController._walkSpeed = player.StartingMoveSpeed;
        // Debug.Log("Stopping push");'
        pushDirectionChanged = false;
        return;
    }
    // public void HandlePush()
    // {
    //     if (playerController.isPulling) return;

    //     // If the player is not against the wall or not moving, reset the push state
    //     if (!playerController._isAgainstWall || movementDirection == Vector3.zero)
    //     {
    //         previousPushDirection = Vector3.zero;
    //         if (pushObstacle != null) pushObstacle.ResetObstacle();
    //         StopPush();
    //         return;
    //     }

    //     // If the player is against the wall and moving
    //     if (playerController._isAgainstWall && movementDirection != Vector3.zero)
    //     {
    //         // Detect the obstacle if not already set
    //         if (pushObstacle == null)
    //         {
    //             if (playerController != null)
    //                 pushObstacle = playerController.FindObstacle();
    //         }

    //         if (pushObstacle != null) pushObstacle.SphereFlags();
    //         // If the obstacle is not pushable, reset and stop pushing
    //         if (pushObstacle == null || !pushObstacle.isPushable)
    //         {
    //             previousPushDirection = Vector3.zero;
    //             if (pushObstacle != null) pushObstacle.ResetObstacle();
    //             StopPush();
    //             return;
    //         }

    //         // Handle obstacle change and reset previous obstacle if needed
    //         if (previousPushObstacle != pushObstacle)
    //         {
    //             if (previousPushObstacle != null) previousPushObstacle.ResetObstacle();
    //             previousPushObstacle = pushObstacle;
    //         }

    //         pushObstacle.SphereFlags();
    //         Debug.Log("Called sphere check");
    //         bool moveable = pushObstacle.CheckObstaclesAround(movementDirection);

    //         // Check if player recently fell and adjust the diff flag
    //         if (playerController.hasRecentlyFallen)
    //         {
    //             diff = Mathf.Round(playerController.fallHeight) - pushObstacle.transform.position.y > 0;
    //         }
    //         else
    //         {
    //             diff = true;
    //         }

    //         // Validate pushing conditions
    //         if (pushObstacle != null && playerController.canPush && moveable && diff && !pushObstacle.pushabilityDelayed)
    //         {
    //             Vector3 directionToObstacle = (pushObstacle.transform.position - transform.position).normalized;
    //             float dotProduct = Vector3.Dot(directionToObstacle, movementDirection);

    //             if (dotProduct > 0.5f) // Ensure player is moving towards the obstacle
    //             {
    //                 if (previousPushDirection == Vector3.zero) 
    //                 {
    //                     // if (!playerController._isJumping)
    //                     // {

    //                     //     previousPushDirection = movementDirection;
    //                     //     Vector3 testSide = pushObstacle.transform.position * pushObstacle.transform.GetComponent<Collider>().bounds.extents.magnitude;
    //                     //     Vector3 cubeSideCenter = pushObstacle.transform.position - pushObstacle.transform.GetComponent<Collider>().bounds.extents.magnitude * movementDirection;
    //                     //     transform.position = cubeSideCenter;

    //                     // }

    //                     Push();
    //                 }
    //                 else if (previousPushDirection == movementDirection)
    //                 {
    //                     // if (!playerController._isJumping)
    //                     // {
    //                     //     Vector3 testSide = pushObstacle.transform.position * pushObstacle.transform.GetComponent<Collider>().bounds.extents.magnitude;
    //                     //     Vector3 cubeSideCenter = pushObstacle.transform.position - pushObstacle.transform.GetComponent<Collider>().bounds.extents.magnitude * movementDirection;
    //                     //     transform.position = cubeSideCenter;
    //                     // }
    //                     Push();
    //                 }
    //                 else
    //                 {
    //                     StopPush();
    //                 }
    //             }
    //             else
    //             {
    //                 StopPush();
    //             }
    //         }
    //         else
    //         {
    //             pushObstacle?.ResetObstacle();
    //             StopPush();
    //         }
    //     }
    // }

    // private void Push()
    // {
    //     Debug.Log("Calling PUSH on obstacle");
    //     playerController.isPushing = true;
    //     pushObstacle.isBeingPushed = true;
    //     pushObstacle.wasRecentlyPushed = true;
    //     Vector3 pushDirection = new Vector3(movementDirection.x, 0, movementDirection.z);
    //     pushObstacle.PushObstacle(pushDirection, player.PushAndPullSpeed(pushObstacle.Weight));
    //     _anim.SetBool("Push", true);
    //     _anim.SetBool("AFK", false);
    // }

    // private void StopPush()
    // {
    //     AudioManager.Instance.StopSound(2);
    //     AudioManager.Instance.StopSound(3);
    //     _anim.SetBool("Push", false);
    //     if (pushObstacle != null) pushObstacle.ResetObstacle();
    //     pushObstacle = null;
    //     previousPushObstacle = null;
    //     previousPushDirection = Vector3.zero;
    //     playerController.isPushing = false;
    //     if (!player.blackHoleDebuff) playerController._walkSpeed = player.StartingMoveSpeed;
    //     return;
    // }

    public float delayTimer;
    private bool started, ended;

    public void HandlePull()
    {
        // playerController.isPushing = false;
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hitCollectible, 0.5f, playerController.collectibleMask))
        {

            if (hitCollectible.transform.TryGetComponent<CollectibleItem>(out var collectibleItem))
            {
                Debug.Log("Looted collectible");
                collectibleItem.Collect(playerController);
            }
        }
        else
        if (Physics.Raycast(ray, out RaycastHit hit, 0.5f, playerController._obstacleMask))
        {
            // Debug.Log("PULL - Obstacle found");
            pullObstacle = hit.transform.GetComponent<Obstacle>();

            if (pullObstacle != null && pullObstacle.grounded && pullObstacle.isPullable && !playerController.recentlyHitWall)
            {
                pullObstacle.SphereFlags();
                if (playerController.isPulling)
                {
                    playerController.canMove = false;
                    pullObstacle.playerController = playerController;
                    pullDirection = -playerController.GetFacingDirection();
                    if (!playerController.grounded)
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
            _anim.SetBool("AFK", false);
            if (playerController.obstacleBehind) return;
            obs.isBeingPulled = true;
            obs.wasRecentlyPushed = true;
            if (obs != null && !playerController.AI) obs.currentlyUsedPlayerConrtoller = playerController;
            float speed = player.PushAndPullSpeed(obs.Weight);
            // Debug.Log("Speed : " + speed);
            // obs.PullObstacle(pullDirection, speed, _rb, playerController.obstacleBehind);
            obs.PullObstacle(pullDirection, speed, playerController.obstacleBehind);
            _rb.MovePosition(_rb.transform.position + pullDirection * speed * Time.fixedDeltaTime);
        }

    }
    void StopPull()
    {
        if (!playerController.AI) AudioManager.Instance.StopObstacleSound_Move();
        playerController.ResetPullConstraints(pullObstacle);
        if (pullObstacle != null) pullObstacle.isBeingPulled = false;
        // pullObstacle.playerController = null;
        if (pullObstacle != null) pullObstacle.currentlyUsedPlayerConrtoller = null;
        pullObstacle = null;
        playerController.canMove = true;
        playerController.StopPull();
        // Debug.Log("PULL - Stopping pull");
        return;
    }
}

