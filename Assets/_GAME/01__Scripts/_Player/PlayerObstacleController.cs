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
    public void HandlePush()
    {
        Debug.Log("Pushing");
        if (playerController.isPulling) return;
        if (playerMovement.justJumpedOutOfPush && _rb.linearVelocity.y > 0.1f)
        {
            return;
        }
        if (!playerController._movement.IsAgainstWall || movementDirection == Vector3.zero)
        {
            previousPushDirection = Vector3.zero;
            if (pushObstacle != null) pushObstacle.ResetObstacle();
            StopPush(); // Ensure stop push logic runs if conditions are no longer met
            return;
        }
        else if (playerController._movement.IsAgainstWall && movementDirection != Vector3.zero)
        {
            // Debug.Log("Pushing");
            pushObstacle = playerController.FindObstacle();
            if (pushObstacle == null)
            {
                // The obstacle is no longer there, or a new one wasn't found
                previousPushDirection = Vector3.zero;
                // No need to call pushObstacle?.ResetObstacle() here because pushObstacle is null
                StopPush();
                return;
            }

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
            Debug.Log("Moveable "+ Moveable + " pushObstacle : " + pushObstacle + " Can push : " + playerController._movement.CanPush);
            // Debug.Log("fall height is : " + playerController.fallHeight);
            // diff = playerController.fallHeight - 0.1f > pushObstacle.transform.position.y
            if (playerController._movement.hasRecentlyFallen)
            {
                diff = Mathf.Round(playerController._movement.fallHeight) - pushObstacle.transform.position.y > 0;
                // Debug.Log("Testing fall - Fall height :" + playerController.fallHeight + " obstacle height " + pushObstacle.transform.position.y);
            }
            else diff = true;
            // Debug.Log("Should the obstacle be pushable :" + (playerController.fallHeight - 0.1f) + " obstacle height" + pushObstacle.transform.position.y);
            if (pushObstacle != null && movementDirection != Vector3.zero && playerController._movement.CanPush && Moveable && diff && !pushObstacle.pushabilityDelayed)
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
            // MODIFICATION: Added '&& Moveable' to ensure push only happens if the obstacle is movable
            else if (!pushObstacle.grounded && pushObstacle.isFalling && !playerController._movement.IsGrounded && Moveable) // Added && Moveable
            {
                // Debug.Log("Code 2");
                Push();
            }
            // MODIFICATION: Added '&& Moveable' to ensure push only happens if the obstacle is movable
            else if (!diff && Moveable) { Debug.Log("Code 3 diff"); Push(); } // Added && Moveable
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