using System.Collections;
using UnityEngine;
using DG.Tweening;
using Obstacles;
public class Obstacle : MonoBehaviour
{

    public MeshRenderer obstacleMesh;
    public Sprite obstacleSprite;
    public ParticleSystem obstacleFallPS;
    public GameObject fallIndicator;
    public Material obstacleMaterial;
    public bool MoveOverride;
    public bool wasSucked;
 
    [SerializeField] public Obstacles.ObstacleType obstacleType;
    public ObstacleAudioType obstacleAudioType;
    public ObstacleColor obstacleColor;
    [Header("Stats & References")]
    public float Weight;
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

        galaxyForward = obstacleForward && _obstacleForward[0] != null && _obstacleForward[0]?.GetComponent<Obstacle>()?.obstacleType == ObstacleType.Galaxy;
        // if (galaxyForward) CheckNeighbourFlags(_obstacleForward[0]?.GetComponent<Obstacle>());
        galaxyBack = obstacleBack && _obstacleBack[0] != null && _obstacleBack[0]?.GetComponent<Obstacle>()?.obstacleType == ObstacleType.Galaxy;
        // if (galaxyBack) CheckNeighbourFlags(_obstacleBack[0]?.GetComponent<Obstacle>());
        galaxyLeft = obstacleLeft && _obstacleLeft[0] != null && _obstacleLeft[0]?.GetComponent<Obstacle>()?.obstacleType == ObstacleType.Galaxy;
        // if (galaxyLeft) CheckNeighbourFlags(_obstacleLeft[0]?.GetComponent<Obstacle>());
        galaxyRight = obstacleRight && _obstacleRight[0] != null && _obstacleRight[0]?.GetComponent<Obstacle>()?.obstacleType == ObstacleType.Galaxy;
        // if (galaxyRight) CheckNeighbourFlags(_obstacleRight[0]?.GetComponent<Obstacle>());

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

        // Debug.DrawRay(transform.position + fallOffset, -transform.up * fallRayDistance, Color.red);
    }

    public bool beingSucked;
  

    private void Start()
    {
        controllerCleared = true;
        if (obstacleType == ObstacleType.Galaxy)
        {
            GameManager.Instance.blackHoleObstacles.Add(this);
        }
        if (obstacleType == ObstacleType.Cardboard)
        {
            GameManager.Instance.jitbObstacles.Add(this);
        }

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
        SphereFlags();
        // Weight = (int)_rb.mass;
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

    private void Reposition()
    {

        if (!isPositioned && tile != null && tile.obstacleDict != null && !spawningBlackHole)
        {
            tile.RepositionObstacle(obs);
        }

    }
    private void HandleFall()
    {
        if (!grounded)
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
            // Debug.Log("Hit ground :" + _boxCollider[0].gameObject.name);
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
            // Debug.Log("Disabling Pushability 0");
            isPushable = false;
        }
        else if (spawningBlackHole)
        {
            // Debug.Log("Disabling Pushability 1");
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

        if (!obstacleUp || recentlyPulled)
        {
            if (MoveOverride) return;
            if (!MoveOverride)
                isPushable = Movable(dir) && !pushabilityDelayed;
            if (!isPushable) return;
          
            if (currentlyUsedPlayerConrtoller != null && currentlyUsedPlayerConrtoller.playerObstacleController != null && currentlyUsedPlayerConrtoller.playerObstacleController.pushDirectionChanged)
            {
                _rb.linearVelocity = Vector3.zero;
                currentlyUsedPlayerConrtoller.playerObstacleController.pushDirectionChanged = false; // Reset the flag
                AudioManager.Instance.StopObstacleSound_Move();
                
                return;
            }
            else
            {
                // dir = 
                dir.y = 0;
                dir.Normalize();
                if (Vector3.Distance(transform.position, tile.transform.position) > 0.001f)
                {
                    if (currentlyUsedPlayerConrtoller != null && !currentlyUsedPlayerConrtoller.AI) AudioManager.Instance.PlayObstacleSound_Move(obstacleAudioType, transform.position);
                    _rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * dir); /*this one doesn't get stuck in cyllinders*/
                   
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
        isPullable = Movable(dir);
        dir.y = 0;
        dir.Normalize();
        if (!obstacleBehind && isPullable && !MoveOverride)
        {

            _rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * dir); /*this one doesn't get stuck in cyllinders*/
            // _playerRigidbody.MovePosition(_playerRigidbody.transform.position + dir * speed * Time.fixedDeltaTime); /*this one doesn't get stuck in cyllinders*/
            if (currentlyUsedPlayerConrtoller != null && !currentlyUsedPlayerConrtoller.AI) AudioManager.Instance.PlayObstacleSound_Move(obstacleAudioType, transform.position);
            // CheckThreeOfAKind();
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
            // Debug.Log("Disablign Pushability");
            yield return new WaitForSeconds(duration);
            // Debug.Log("enabling pushability");
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
    public IEnumerator Fall()
    {
        StartCoroutine(DisablePushabilityForTime(0.5f));

        if (!hasFallStarted)
        {

            hasFallStarted = true;
            startFallPosition = transform.position;
            // if (GetComponent<AStarTestMove>() != null) GetComponent<AStarTestMove>().wasPushed = true;
            if (!recentlyFallen) StartCoroutine(FallDelayTimer());
            // Mathf.RoundToInt(targetHeight);
            RaycastHit hit;
            float raycastDistance = 45f;  // Adjust this distance based on your scene
            float groundHeight = 0.554f;  // Default ground height if raycast doesn't hit anything
            float targetHeight;
            // Cast a ray downward to find the ground
            if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, _groundMask))
            {
                Vector3 pos = hit.point;
                Transform tr = hit.transform;
                if (fallSprite == null)
                {
                    fallSprite = Instantiate(fallIndicator, pos + new Vector3(0, 0.05f, 0), fallIndicator.transform.rotation);
                    // Debug.Log("fallsprite scale at start   :   " + fallSprite.transform.localScale);
                    // if (obstacleMaterial != null)
                    //     fallSprite.GetComponent<SpriteRenderer>().color = obstacleMaterial.color;
                }
                if (hit.transform.CompareTag("Tile"))
                    groundHeight = hit.transform.position.y + tileOffset;
                else if (hit.transform.CompareTag("Obstacle") || hit.transform.CompareTag("OtherObstacle"))
                    groundHeight = hit.transform.position.y + obstacleOffset;

                targetHeight = groundHeight;
            }
            targetHeight = groundHeight;
            Vector3 startFallIndicatorScale = fallSprite.transform.localScale;
            while (transform.position.y > targetHeight || !grounded)
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
                // fallSprite.transform.localScale = startFallIndicatorScale / (targetHeight - transform.position.y);

                float distanceToGround = Mathf.Abs(Vector3.Distance(new Vector3(transform.position.x, targetHeight, transform.position.z), transform.position));


                float scaleFactor = startFallIndicatorScale.x + (20f - distanceToGround) / 25f;
                float scalingMultiplier = 11.5f; // Adjust this value to control the overall scaling effect
                if (fallSprite != null)
                {
                    fallSprite.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
                    // Debug.Log("fallsprite MID at start   :   " + fallSprite.transform.localScale);
                    fallSprite.transform.localScale = startFallIndicatorScale * scaleFactor * scalingMultiplier;
                    // Debug.Log("fallsprite LATE at start   :   " + fallSprite.transform.localScale);
                }

                transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, targetHeight, transform.position.z), fallSpeed * 17.5f);
                // if (playerController == null) Debug.Log("Player controller is null");
                // if (playerController.IsGrounded) Debug.Log("Player is grounded");
                // Debug.Log("IsGrounded " + playerController.IsGrounded);
                if (playerController != null && !playerController.IsGrounded)
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
            if (transform.position.y <= targetHeight)
            {

                isFalling = false;
                hasFallStarted = false;
                transform.position = new Vector3(transform.position.x, targetHeight, transform.position.z);
                ParticleSystem ps;
                if (obstacleFallPS != null && transform.position.y < 1)
                {

                    ps = Instantiate(obstacleFallPS, transform.position - new Vector3(0, 0.5f, 0), obstacleFallPS.transform.rotation);
                    ps.gameObject.name = "fall particle object";
                }
            }
            // Debug.Log("fallsprite LATE at start   :   " + fallSprite.transform.localScale);
            hasFallStarted = false;
            isFalling = false;
            tile.PositionHeight(this);
            // Debug.Log("We hit ground " + groundHeight);
            if (GetComponent<MatchThreeObstacle>() != null && GetComponent<MatchThreeObstacle>().isDestructible)
            {

                GetComponent<MatchThreeObstacle>().RunMatchOnce();
            }
            // Debug.Log("Run match once ");
            Destroy(fallSprite, 0.1f);

        }

        // isPositioned = false;
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
            if (hit.transform != transform) // Checks if the hit obstacle is the same as this obstacle.
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

    public ParticleSystem destructionParticleSystem;
    public bool queuedForDestruction;

    public bool AIObstacle;
    public AudioClip audioClip;
    public GoalSetter[] goalSetters;
    public AudioSource audioSource;
    public void ParticleDestroy()
    {
        StartCoroutine(DelayedParticleDestroy());
    }
    IEnumerator DelayedParticleDestroy()
    {
        // DELAY ON DESTRUCTION/SOUND change as needed (was 0.1f)
        yield return new WaitForSeconds(0.05f);  // Adjust the delay as needed

        if (!queuedForDestruction)
        {
            queuedForDestruction = true;
            ParticleSystem ps = Instantiate(destructionParticleSystem, new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z), destructionParticleSystem.transform.rotation, null);
            ps.gameObject.name = " PARTICLE OBJECT";
            gameObject.SetActive(false);
            // ps.GetComponent<ParticleSystemRenderer>().material = GetComponent<Renderer>().material;
            // ps.transform.GetChild(0).GetComponent<ParticleSystem>().GetComponent<ParticleSystemRenderer>().material = GetComponent<Renderer>().material;
            ps.Play();


            // GameObject audioObject = Instantiate(new GameObject(), transform.position, transform.rotation);
            // audioObject.name = " AUDIO OBJECT ";
            // audioObject.AddComponent<AudioSource>();
            // if (audioClip != null)
            //     audioObject.GetComponent<AudioSource>().clip = audioClip;
            // else audioObject.GetComponent<AudioSource>().clip = AudioManager.Instance.SoundAudioSources[0].clip;
            // audioObject.GetComponent<AudioSource>().volume = 0.40f;
            // if (obstacleType == ObstacleInfo.ObstacleType.Concrete) audioObject.GetComponent<AudioSource>().volume = 0.65f;
            // if (obstacleType == ObstacleInfo.ObstacleType.Metal) audioObject.GetComponent<AudioSource>().volume = 0.35f;
            // Debug.Log("Obstacle name :" + name + " obstacle type : " + obstacleType + " audio volume  :" + audioObject.GetComponent<AudioSource>().volume);
            // audioObject.GetComponent<AudioSource>().Play();
            // Destroy(audioObject, audioObject.GetComponent<AudioSource>().clip.length);

            AudioManager.Instance.PlayObstacleSound_Destruction(obstacleAudioType, transform.position);

            // audioSource = GetComponent<AudioSource>();
            // audioSource.clip = AudioManager.Instance.SoundAudioSources[0].clip;
            // // audioClip = audioSource.clip;
            // audioSource.PlayOneShot(audioSource.clip);
            // Debug.Log("Playing Sound :" + audioSource.clip.name);
            // AudioManager.Instance.PlaySoundObstacleDestroy(obstacleAudioType);
            if (fallSprite != null) Destroy(fallSprite);
            if (GameManager.Instance.levelGoal != null)
            {
                GameManager.Instance.levelGoal.RemoveObstacleFromSection(this);
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

                        {
                            gs.playerGoals[i].IncreaseCount();



                        }
                    }
                }
            }



            Destroy(gameObject);
        }
    }

    public void ResetObstacle()
    {

        isBeingPushed = false;
        isBeingPulled = false;
        isPositioned = false;
        isHeightPositioned = false;
        isPullable = true;
    }

    public bool galaxyForward, galaxyBack, galaxyLeft, galaxyRight, threeOfAKind, spawningBlackHole;
    private const float CenterThreshold = 0.05f;  

    public bool onDestroy;
    public GameObject objectToTurnOff;
    public GameObject objectToTurnOn;

    public void OnDestroy()
    {
        if (gameObject.scene.isLoaded && QuestRotator.Instance != null) //Was Deleted
        {
            QuestRotator.Instance.UpdateQuestProgress(QuestType.Destroy);

        }
        else //Was Cleaned Up on Scene Closure
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

        // Revert to color1 after blinking period is over
        obstacleMaterial.color = startColor;
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

}

