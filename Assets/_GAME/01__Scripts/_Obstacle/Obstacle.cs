using System.Collections;
using UnityEngine;
using DG.Tweening;
using Coherence;
using Coherence.Toolkit;

public class Obstacle : MonoBehaviour
{
    public bool is_x_box;
    public bool isFrozen;
    public MeshRenderer obstacleMesh;
    public Sprite obstacleSprite;
    public ParticleSystem obstacleFallPS;
    public GameObject fallIndicator;
    public Material obstacleMaterial;
    public bool MoveOverride;
    public bool isSpawnedForRecovery = false;
    public bool shouldFall = true;
    public ObstacleType obstacleType;
    public ObstacleModifier obstacleModifier;
    public ObstacleColor obstacleColor;
    public ObstacleAudioType obstacleAudioType;
    [Header("Stats & References")]
    public float Weight;                 // base value, authored on the prefab — never mutated at runtime
    private float _weightModifier = 1f;  // captured once from the level

    /// <summary>
    /// Prefab Weight scaled by the level-wide modifier. Gameplay (push/pull speed)
    /// should always read this, never the raw Weight.
    /// </summary>
    public float EffectiveWeight => Weight * _weightModifier;
    public Tile tile;
    public GameObject fracturedObject;
    public PlayerController playerController;
    public Renderer rend;
    Obstacle obs;
    public bool obstacleForward, obstacleBack, obstacleLeft, obstacleRight, obstacleUp;
    public bool isPushable, isPullable, isBeingPushed, isBeingPulled, wasRecentlyPushed, wasRecentlyPulled;
    public bool isFalling;
    public bool isPositioned, isHeightPositioned;
    public bool grounded;
    public bool isHammerable;
    [SerializeField] public Rigidbody _rb;
    private CoherenceSync _coherenceSync;

    public float obstacleCheckerRadius, upOffset, upSphereDistance;
    public float FallTimer;
    public Vector3 forwardOffset_1, forwardOffset_2, forwardOffset_3, forwardOffset_4;
    public Vector3 backOffset_1, backOffset_2, backOffset_3, backOffset_4;
    public Vector3 leftOffset_1, leftOffset_2, leftOffset_3, leftOffset_4;
    public Vector3 rightOffset_1, rightOffset_2, rightOffset_3, rightOffset_4;
    public bool hasFallStarted;
    public Collider[] _obstacleForward = new Collider[1];
    public Collider[] _obstacleBack = new Collider[1];
    public Collider[] _obstacleLeft = new Collider[1];
    public Collider[] _obstacleRight = new Collider[1];
    public Collider[] _obstacleUp = new Collider[1];
    public Collider[] _grounded = new Collider[1];
    public Collider[] _boxCollider = new Collider[1];
    [SerializeField] private LayerMask _obstacleMask;
    public bool isMoving;
    public bool Moving;

    public void TakeAuthority()
    {
        _coherenceSync.RequestAuthority(AuthorityType.Full);
    }

    public bool CheckObstaclesAround(Vector3 dir)
    {
        if (!MoveOverride)
            return Movable(dir);
        else return false;
    }
    public void SphereFlags()
    {
        Physics.SyncTransforms();
        obstacleForward = Physics.OverlapSphereNonAlloc(transform.position + forwardOffset_1, obstacleCheckerRadius, _obstacleForward, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + forwardOffset_2, obstacleCheckerRadius, _obstacleForward, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + forwardOffset_3, obstacleCheckerRadius, _obstacleForward, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + forwardOffset_4, obstacleCheckerRadius, _obstacleForward, _groundMask) > 0;
        obstacleBack = Physics.OverlapSphereNonAlloc(transform.position + backOffset_1, obstacleCheckerRadius, _obstacleBack, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + backOffset_2, obstacleCheckerRadius, _obstacleBack, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + backOffset_3, obstacleCheckerRadius, _obstacleBack, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + backOffset_4, obstacleCheckerRadius, _obstacleBack, _groundMask) > 0;
        obstacleLeft = Physics.OverlapSphereNonAlloc(transform.position + leftOffset_1, obstacleCheckerRadius, _obstacleLeft, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + leftOffset_2, obstacleCheckerRadius, _obstacleLeft, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + leftOffset_3, obstacleCheckerRadius, _obstacleLeft, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + leftOffset_4, obstacleCheckerRadius, _obstacleLeft, _groundMask) > 0;
        obstacleRight = Physics.OverlapSphereNonAlloc(transform.position + rightOffset_1, obstacleCheckerRadius, _obstacleRight, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + rightOffset_2, obstacleCheckerRadius, _obstacleRight, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + rightOffset_3, obstacleCheckerRadius, _obstacleRight, _groundMask) > 0 || Physics.OverlapSphereNonAlloc(transform.position + rightOffset_4, obstacleCheckerRadius, _obstacleRight, _groundMask) > 0;
        obstacleUp = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(upSphereDistance, upOffset, 0f), obstacleCheckerRadius, _obstacleUp, _groundMask) > 0;
        if (_obstacleForward[0] == gameObject)
        {
            _obstacleForward[0] = null;
        }
        if (_obstacleBack[0] == gameObject)
        {
            _obstacleBack[0] = null;
        }
        if (_obstacleLeft[0] == gameObject)
        {
            _obstacleLeft[0] = null;
        }
        if (_obstacleRight[0] == gameObject)
        {
            _obstacleRight[0] = null;
        }
        if (_obstacleUp[0] == gameObject)
        {
            _obstacleUp[0] = null;
        }
    }

    public float fallRayDistance;
    public Vector3 fallOffset;
    private void DrawObstacleGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + forwardOffset_1, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + forwardOffset_2, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + forwardOffset_3, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + forwardOffset_4, obstacleCheckerRadius);

        Gizmos.DrawWireSphere(transform.position + backOffset_1, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + backOffset_2, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + backOffset_3, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + backOffset_4, obstacleCheckerRadius);

        Gizmos.DrawWireSphere(transform.position + leftOffset_1, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + leftOffset_2, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + leftOffset_3, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + leftOffset_4, obstacleCheckerRadius);

        Gizmos.DrawWireSphere(transform.position + rightOffset_1, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + rightOffset_2, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + rightOffset_3, obstacleCheckerRadius);
        Gizmos.DrawWireSphere(transform.position + rightOffset_4, obstacleCheckerRadius);
    }

    public bool beingSucked;

    private void Start()
    {
        controllerCleared = true;
        if (GameManager.Instance != null)
            _weightModifier = GameManager.Instance.ObstacleWeightModifier;

        FallTimer = 0f;
        if (GetComponent<Rigidbody>() != null)
        {
            _rb = GetComponent<Rigidbody>();
        }
        if (GetComponent<Renderer>() != null)
        {
            rend = GetComponent<Renderer>();
        }
        obs = GetComponent<Obstacle>();
        _coherenceSync = GetComponent<CoherenceSync>();
        if (_coherenceSync != null && !_coherenceSync.HasStateAuthority)
        {
            if (_rb != null) _rb.isKinematic = true;
        }
        SphereFlags();
        isPullable = true;
        if (obstacleMesh != null)
            obstacleMaterial = obstacleMesh.sharedMaterial;

        if (obstacleMaterial == null)
        {
            Debug.LogWarning("Object material not set.");
        }
        else if (shouldBlink)
        {
            StartCoroutine(BlinkCoroutine());
        }
    }

    private bool controllerCleared;

    private void FixedUpdate()
    {
        Moving = isFalling || isBeingPulled || isBeingPushed || !isPositioned;

        if (_coherenceSync != null && !_coherenceSync.HasStateAuthority)
        {
            UpdateProxyFallIndicator();
            return;
        }

        Reposition();
        CheckUp();
        CheckGround();
        HandleFall();
        if (!isBeingPushed && !isBeingPulled && !controllerCleared)
        {
            Debug.Log("Clearing player controller");
            playerController = null;
            controllerCleared = true;
        }
    }

    private Vector3 proxyFallSpriteStartScale;
    private void UpdateProxyFallIndicator()
    {
        if (isFalling && fallSprite == null && fallIndicator != null)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 45f, _groundMask))
            {
                fallSprite = Instantiate(fallIndicator, hit.point + new Vector3(0, 0.05f, 0), fallIndicator.transform.rotation);
                proxyFallSpriteStartScale = fallSprite.transform.localScale;
            }
        }
        else if (!isFalling && fallSprite != null)
        {
            Destroy(fallSprite);
            fallSprite = null;
        }
        else if (isFalling && fallSprite != null)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 45f, _groundMask))
            {
                fallSprite.transform.position = hit.point + new Vector3(0, 0.05f, 0);

                float targetHeight = hit.point.y;
                if (hit.transform.CompareTag("Tile"))
                    targetHeight = hit.transform.position.y + tileOffset;
                else if (hit.transform.CompareTag("Obstacle") || hit.transform.CompareTag("OtherObstacle"))
                    targetHeight = hit.transform.position.y + obstacleOffset;

                float distanceToGround = Mathf.Abs(Vector3.Distance(new Vector3(transform.position.x, targetHeight, transform.position.z), transform.position));
                float scaleFactor = proxyFallSpriteStartScale.x + (20f - distanceToGround) / 25f;
                float scalingMultiplier = 11.5f;
                fallSprite.transform.localScale = proxyFallSpriteStartScale * scaleFactor * scalingMultiplier;
                if (fallSprite.transform.localScale.x > 0.11f)
                    fallSprite.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
            }
        }
    }

    private void Reposition()
    {
        if (!isPositioned && tile != null && tile.obstacleDict != null)
        {
            Debug.Log("Repositioning");
            tile.RepositionObstacle(obs);
        }
    }

    private void HandleFall()
    {
        if (!grounded && shouldFall)
        {
            StartCoroutine(Fall());
        }
    }

    [SerializeField] private LayerMask _groundMask;
    [SerializeField] float _grounderOffset;
    [SerializeField] float _grounderRadius;
    public bool CheckForCollisions()
    {
        _boxCollider = Physics.OverlapBox(transform.position + BoxOffset, HalfExtents, transform.rotation, _groundMask);
        if (_boxCollider.Length == 1)
        {
            if (_boxCollider[0].gameObject == gameObject)
                return false;
            return true;
        }
        if (_boxCollider.Length > 0)
            return true;
        return false;
    }

    #region Grounding
    public Vector3 BoxOffset, BoxSize;
    public Vector3 HalfExtents = new(0.25f, 0.25f, 0.25f);
    private void CheckGround()
    {
        grounded = CheckForCollisions();
        if (!grounded)
        {
            isPullable = false;
            isPositioned = false;
            isHeightPositioned = false;
        }
        else if (spawningBlackHole) isPullable = false;
        else isPullable = true;
    }
    private void DrawBoxGizmo()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(gameObject.transform.position + BoxOffset, HalfExtents);
    }

    private void DrawGrounderGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, _grounderOffset), _grounderRadius);
    }
    #endregion

    private void CheckUp()
    {
        if (obstacleUp || pushabilityDelayed)
        {
            isPushable = false;
        }
        else if (!MoveOverride) isPushable = true;
    }

    public bool Movable(Vector3 playerDir)
    {
        if (playerDir.z == 1 && obstacleForward) return false;
        else if (playerDir.z == -1 && obstacleBack) return false;
        else if (playerDir.x == 1 && obstacleRight) return false;
        else if (playerDir.x == -1 && obstacleLeft) return false;
        else return true;
    }

    private void OnDrawGizmos()
    {
        DrawGrounderGizmos();
        DrawObstacleGizmos();
        DrawBoxGizmo();
    }

    public float priority;
    public bool hasPriority;
    public Obstacle GetObstacleInDirection(Vector3 direction)
    {
        if (direction == Vector3.forward)
        {
            if (_obstacleForward[0] != null)
                return _obstacleForward[0].GetComponent<Obstacle>();
            else return null;
        }
        else if (direction == Vector3.back)
        {
            if (_obstacleBack[0] != null)
                return _obstacleBack[0].GetComponent<Obstacle>();
            else return null;
        }
        else if (direction == Vector3.left)
        {
            if (_obstacleLeft[0] != null)
                return _obstacleLeft[0].GetComponent<Obstacle>();
            else return null;
        }
        else if (direction == Vector3.right)
        {
            if (_obstacleRight[0] != null)
                return _obstacleRight[0].GetComponent<Obstacle>();
            else return null;
        }
        else return null;
    }

    public PlayerController currentlyUsedPlayerConrtoller;
    public void PushObstacle(Vector3 dir, float speed)
    {
        if (_coherenceSync != null && !_coherenceSync.HasStateAuthority) return;

        Debug.Log("Pushing Obstacle");
        if (!obstacleUp || recentlyPulled)
        {
            if (MoveOverride) return;
            if (!MoveOverride)
                isPushable = Movable(dir) && !pushabilityDelayed;
            if (!isPushable) return;

            if (currentlyUsedPlayerConrtoller != null && currentlyUsedPlayerConrtoller.playerObstacleController != null && currentlyUsedPlayerConrtoller.playerObstacleController.pushDirectionChanged)
            {
                _rb.linearVelocity = Vector3.zero;
                currentlyUsedPlayerConrtoller.playerObstacleController.pushDirectionChanged = false;
                AudioManager.Instance.StopObstacleSound_Move();
                return;
            }
            else
            {
                dir.y = 0;
                dir.Normalize();
                if (Vector3.Distance(transform.position, tile.transform.position) > 0.001f)
                {
                    Debug.Log("Moving");
                    if (currentlyUsedPlayerConrtoller != null && !currentlyUsedPlayerConrtoller.AI)
                    {
                        Debug.Log("Playing obstacle sound move");
                        AudioManager.Instance.PlayObstacleSound_Move(obstacleAudioType, transform.position);
                    }
                    _rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * dir);
                }
                else
                {
                    if (currentlyUsedPlayerConrtoller != null && !currentlyUsedPlayerConrtoller.AI) AudioManager.Instance.StopObstacleSound_Move();
                    return;
                }
            }
            controllerCleared = false;
        }
    }

    public void PullObstacle(Vector3 dir, float speed, bool obstacleBehind)
    {
        if (_coherenceSync != null && !_coherenceSync.HasStateAuthority) return;

        isPullable = Movable(dir);
        dir.y = 0;
        dir.Normalize();
        if (!obstacleBehind && isPullable && !MoveOverride)
        {
            _rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * dir);
            if (currentlyUsedPlayerConrtoller != null && !currentlyUsedPlayerConrtoller.AI) AudioManager.Instance.PlayObstacleSound_Move(obstacleAudioType, transform.position);
            controllerCleared = false;
        }
        else
        {
            if (currentlyUsedPlayerConrtoller != null && !currentlyUsedPlayerConrtoller.AI) AudioManager.Instance.StopObstacleSound_Move();
            controllerCleared = false;
            return;
        }
    }

    public bool recentlyPulled;

    public bool pushabilityDelayed;
    private IEnumerator DisablePushabilityForTime(float duration)
    {
        if (!isFalling)
        {
            isFalling = true;
            isPushable = false;
            pushabilityDelayed = true;
            yield return new WaitForSeconds(duration);
            pushabilityDelayed = false;
            isPushable = true;
        }
    }

    public float fallSpeed = 1f;
    public Vector3 startFallPosition;
    public bool recentlyFallen;
    GameObject fallSprite = null;
    float tileOffset = 0.554f;
    float obstacleOffset = 1f;
    private Coroutine fallCouroutine;
    public IEnumerator Fall()
    {
        StartCoroutine(DisablePushabilityForTime(0.5f));

        if (!hasFallStarted)
        {
            hasFallStarted = true;
            startFallPosition = transform.position;
            if (!recentlyFallen) StartCoroutine(FallDelayTimer());
            RaycastHit hit;
            float raycastDistance = 45f;
            float groundHeight = 0.554f;
            float targetHeight;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, _groundMask))
            {
                Vector3 pos = hit.point;
                Transform tr = hit.transform;
                if (fallSprite == null)
                {
                    fallSprite = Instantiate(fallIndicator, pos + new Vector3(0, 0.05f, 0), fallIndicator.transform.rotation);
                }
                if (hit.transform.CompareTag("Tile"))
                    groundHeight = hit.transform.position.y + tileOffset;
                else if (hit.transform.CompareTag("Obstacle") || hit.transform.CompareTag("OtherObstacle"))
                    groundHeight = hit.transform.position.y + obstacleOffset;

                targetHeight = groundHeight;
            }
            targetHeight = groundHeight;
            Vector3 startFallIndicatorScale = fallSprite.transform.localScale;
            while ((transform.position.y > targetHeight || !grounded) && !isFrozen)
            {
                if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, _groundMask))
                {
                    if (fallSprite != null)
                    {
                        Vector3 pos = hit.point;
                        fallSprite.transform.position = pos + new Vector3(0, 0.05f, 0);
                    }
                    if (hit.transform.CompareTag("Tile"))
                        groundHeight = hit.transform.position.y + tileOffset;
                    else if (hit.transform.CompareTag("Obstacle") || hit.transform.CompareTag("OtherObstacle"))
                        groundHeight = hit.transform.position.y + obstacleOffset;
                    targetHeight = groundHeight;
                }
                if (fallSprite != null && fallSprite.transform.localScale.x > 0.11f)
                    fallSprite.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);

                float distanceToGround = Mathf.Abs(Vector3.Distance(new Vector3(transform.position.x, targetHeight, transform.position.z), transform.position));

                float scaleFactor = startFallIndicatorScale.x + (20f - distanceToGround) / 25f;
                float scalingMultiplier = 11.5f;
                if (fallSprite != null)
                {
                    fallSprite.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
                    fallSprite.transform.localScale = startFallIndicatorScale * scaleFactor * scalingMultiplier;
                    if (fallSprite.transform.localScale.x > 0.11f)
                        fallSprite.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
                }

                transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, targetHeight, transform.position.z), fallSpeed * 17.5f);
                if (playerController != null && !playerController._movement.IsGrounded)
                {
                    Debug.Log("Falling with box");
                    playerController.transform.position = Vector3.MoveTowards(playerController.transform.position, new Vector3(playerController.transform.position.x, transform.position.y, playerController.transform.position.z), fallSpeed * 16);
                }
                yield return new WaitForSeconds(0.01f);
                if (transform.position.y <= targetHeight && !grounded)
                {
                    targetHeight -= 0.1f;
                }
            }
            if (isFrozen)
            {
                isFalling = false;
                yield break;
            }
            if (transform.position.y <= targetHeight)
            {
                isFalling = false;
                hasFallStarted = false;
                transform.position = new Vector3(transform.position.x, targetHeight, transform.position.z);
                if (obstacleFallPS != null && transform.position.y < 1)
                {
                    if (_coherenceSync != null)
                        _coherenceSync.SendCommand<Obstacle>(nameof(CmdPlayFallLanding), MessageTarget.All, transform.position);
                    else
                    {
                        ParticleSystem ps = Instantiate(obstacleFallPS, transform.position - new Vector3(0, 0.5f, 0), obstacleFallPS.transform.rotation);
                        ps.gameObject.name = "fall particle object";
                    }
                }
            }
            hasFallStarted = false;
            isFalling = false;
            tile.PositionHeight(this);
            if (GetComponent<MatchThreeObstacle>() != null && GetComponent<MatchThreeObstacle>().isDestructible)
            {
                GetComponent<MatchThreeObstacle>().RunMatchOnce();
            }
            Destroy(fallSprite, 0.1f);
        }
    }

    [Command(defaultRouting = MessageTarget.All)]
    public void CmdPlayFallLanding(Vector3 landPosition)
    {
        if (obstacleFallPS != null && landPosition.y < 1)
        {
            ParticleSystem ps = Instantiate(obstacleFallPS, landPosition - new Vector3(0, 0.5f, 0), obstacleFallPS.transform.rotation);
            ps.gameObject.name = "fall particle object";
        }
    }

    public IEnumerator FallDelayTimer()
    {
        recentlyFallen = true;
        yield return new WaitForSeconds(1f);
        recentlyFallen = false;
    }

    public void CheckRaycastIntersection(Transform playerEyeTransform, LayerMask obstacleMask)
    {
        RaycastHit hit;
        if (Physics.Raycast(playerEyeTransform.position, playerEyeTransform.forward, out hit, 0.5f, obstacleMask))
        {
            if (hit.transform != transform)
            {
                ClearPlayerController();
            }
        }
        else
        {
            ClearPlayerController();
        }
    }

    public void ClearPlayerController()
    {
        Debug.Log("Clearing palyer controller in func");
        playerController = null;
    }

    // not used anymore, obstacleDestructionVfxPrefab is used instead, but keeping it for easy wiring of obstacleDestructionVfxPrefab
    // using CreateObstacleDestructionVfxPrefabs editor tool
    public ParticleSystem destructionParticleSystem;
    public ObstacleDestructionVfx obstacleDestructionVfxPrefab;
    public bool queuedForDestruction;

    public bool AIObstacle;
    public AudioClip audioClip;
    public GoalSetter[] goalSetters;
    public AudioSource audioSource;

    public void ParticleDestroy(ObstacleDestructionSource source = ObstacleDestructionSource.None)
    {
        if (queuedForDestruction) return;
        if (_coherenceSync != null && !_coherenceSync.HasStateAuthority) return;
        queuedForDestruction = true;
        StartCoroutine(DestroyRoutine(source));
    }

    private IEnumerator DestroyRoutine(ObstacleDestructionSource source)
    {
        if (source != ObstacleDestructionSource.None && obstacleDestructionVfxPrefab != null)
        {
            Vector3 vfxPos = new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z);
            ObstacleDestructionVfx vfx = Instantiate(obstacleDestructionVfxPrefab, vfxPos, obstacleDestructionVfxPrefab.transform.rotation);
            vfx.ObstacleAudioType = obstacleAudioType;
        }

        yield return new WaitForSeconds(0.05f);

        if (fallSprite != null) Destroy(fallSprite);

        if (GameManager.Instance.levelGoal != null)
        {
            GameManager.Instance.levelGoal.RemoveObstacleFromSection(this);
            if (source != ObstacleDestructionSource.MatchThree)
                GameManager.Instance.levelGoal.QueueObstacleForSpawnProcessing(this);
        }
        if (GameManager.Instance.levelGoal.DualLevel)
        {
            if (!AIObstacle)
            {
                if (obstacleType == ObstacleType.Universal) yield return null;
                GoalSetter gs = GameManager.Instance.playerGoalSetter;
                if (gs.obstacleTypes[0].obstacleType == obstacleType)
                {
                    gs.FillBar();
                    Debug.Log("Filling AI");
                }
            }
            else
            {
                GoalSetter gs = GameManager.Instance.AIGoalSetter;
                if (gs.obstacleTypes[0].obstacleType == obstacleType)
                {
                    gs.FillBar();
                    Debug.Log("Filling player");
                }
            }
        }
        else
        {
            GoalSetter gs = FindObjectOfType<GoalSetter>();
            if (gs != null)
            {
                for (int i = 0; i < gs.playerGoals.Count; i++)
                {
                    if (gs.playerGoals[i].obstacleType == obstacleType)
                        gs.playerGoals[i].IncreaseCount();
                }
            }
        }

        if (playerController != null) playerController.playerObstacleController.pushObstacle = null;
        Destroy(gameObject);
    }

    public void ResetObstacle()
    {
        isBeingPushed = false;
        isBeingPulled = false;
        isPositioned = false;
        isHeightPositioned = false;
        isPullable = true;
        Debug.Log("Resetting obstacle :" + gameObject.name);
    }

    public bool galaxyForward, galaxyBack, galaxyLeft, galaxyRight, threeOfAKind, spawningBlackHole;
    private const float CenterThreshold = 0.05f;

    public bool onDestroy;
    public GameObject objectToTurnOff;
    public GameObject objectToTurnOn;

    public void OnDestroy()
    {
        // fallSprite is a standalone instance, not a child of this obstacle, so it
        // isn't cleaned up automatically. Destroying mid-fall (player attack,
        // match-three, network despawn on proxies) otherwise orphans the indicator.
        if (fallSprite != null)
        {
            Destroy(fallSprite);
            fallSprite = null;
        }

        if (gameObject.scene.isLoaded && QuestRotator.Instance != null)
        {
            if (_coherenceSync == null || _coherenceSync.HasStateAuthority)
                QuestRotator.Instance.UpdateQuestProgress(QuestType.Destroy);
        }
        else
        {
            Debug.Log("Cleaning scene so not updating");
        }
        Debug.Log("Added Osbtacle");
        if (onDestroy && objectToTurnOff != null)
        {
            objectToTurnOff.SetActive(false);
        }
    }

    public Color startColor, endColor;
    public float blinkDuration = 1f;
    public float blinkTime = 5.0f;
    public bool shouldBlink;
    IEnumerator BlinkCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < blinkTime)
        {
            yield return StartCoroutine(BlinkToColor(startColor, endColor, blinkDuration / 2));
            elapsedTime += blinkDuration / 2;

            if (elapsedTime < blinkTime)
            {
                yield return StartCoroutine(BlinkToColor(endColor, startColor, blinkDuration / 2));
                elapsedTime += blinkDuration / 2;
            }
        }

        obstacleMaterial.color = startColor;
    }

    public void FreezeFall(bool freeze)
    {
        isFrozen = freeze;
        isFalling = !freeze;
        shouldFall = !freeze;
        isPositioned = !freeze;
        hasFallStarted = freeze;
        if (freeze) Debug.Log("Freezing");
        else Debug.Log("Unfreezing");
    }

    IEnumerator BlinkToColor(Color fromColor, Color toColor, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            obstacleMaterial.color = Color.Lerp(fromColor, toColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        obstacleMaterial.color = toColor;
    }

    public enum ObstacleDestructionSource
    {
        None,
        MatchThree,
        Weapon,
        Bomb,
        Other
    }
}
