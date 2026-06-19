using System;
using System.Collections;
using System.Collections.Generic;
using Coherence;
using Coherence.Toolkit;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
public class Rocket : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    public SphereCollider _playerTrigger;
    public CapsuleCollider _obstacleDestructionTrigger;
    private Rigidbody _rb;
    // private Animator _animator;
    private CoherenceSync _coherenceSync;
    private const string PlayerTag = "Player";
    private const string ObstacleTag = "Obstacle";
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float acceleration = 5f;
    public GameObject spriteCanvas;
    [SerializeField] private bool _isLaunched = false;
    public List<Image> spriteFillList = new();
    private float _currentSpeed = 0f;
    [SerializeField] bool Grounded;
    [SerializeField] Color spriteTargetColor;
    [SerializeField] Color spriteStartColor = Color.white;
    [SerializeField] GameObject _fireParticleObject;
    [SerializeField] GameObject _smokeParticleObject;
    private String rocketDisablerTag = "RocketDisabler";
    private bool isDisabledForMultiplayer;
    // The player whose touch launched this rocket. Used in local bot matches to
    // scope destruction to that player's side (there's no Coherence authority to
    // do it offline).
    private Player _launcher;
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // _animator = GetComponent<Animator>();
        _coherenceSync = GetComponent<CoherenceSync>();
        // Get the animator's transform for launch direction
        if (_animator != null)
        {
            launchDirectionTransform = _animator.transform;
        }
        else
        {
            Debug.LogError("Animator component not found!");
            launchDirectionTransform = transform; // fallback to parent transform
        }

        if (_playerTrigger == null || _obstacleDestructionTrigger == null || _rb == null || _animator == null)
        {
            Debug.LogError("Required components are missing on the Rocket GameObject.");
        }
    }
    void Update()
    {
        Grounded = CheckForCollisions();
        if (!Grounded) ApplyFakeGravity();
        if (!_isLaunched)
        {
            StartCoroutine(AnimateArrowColors());
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PlayerTag) && !_isLaunched)
        {
            _launcher = other.GetComponent<Player>();
            if (_coherenceSync == null || _coherenceSync.HasStateAuthority)
            {
                LaunchRocket();
                Debug.Log("Rocket Launch DETECTED");
                if (_coherenceSync != null)
                    _coherenceSync.SendCommand<Rocket>(nameof(CmdLaunchRocket), MessageTarget.Other);
            }
        }
        if (other.CompareTag(rocketDisablerTag))
        {

            isDisabledForMultiplayer = true;
            Debug.Log("Rocket Disabled");

        }
        if (other.CompareTag(ObstacleTag) && _isLaunched && !isDisabledForMultiplayer)
        {
            Obstacle obstacle = other.GetComponent<Obstacle>();
            if (obstacle == null) return;

            // Local bot match: there's no Coherence authority to scope this to one
            // side (offline every entity reports authority), so gate by side
            // explicitly — a rocket only clears obstacles on its launcher's half.
            if (GameManager.Instance != null && GameManager.Instance.IsBotMatch)
            {
                if (!ObstacleOnLauncherSide(obstacle)) return;
            }
            else if (_coherenceSync != null)
            {
                CoherenceSync obstacleSync = obstacle.GetComponent<CoherenceSync>();
                if (obstacleSync == null || !obstacleSync.HasStateAuthority) return;
            }

            obstacle.ParticleDestroy(Obstacle.ObstacleDestructionSource.Weapon);
        }
    }
    /// <summary>
    /// True if the obstacle belongs to the goal of the player who launched this
    /// rocket, per the LevelGoal lists: the bot's obstacles are in
    /// ObstaclesToDestroy_Opponent, the human's in ObstaclesToDestroy_Player.
    /// A rocket only clears its launcher's own goal obstacles. Used only in local
    /// bot matches, replacing the Coherence authority gate that scopes destruction
    /// to one side in real multiplayer.
    /// </summary>
    private bool ObstacleOnLauncherSide(Obstacle obstacle)
    {
        LevelGoal levelGoal = GameManager.Instance.levelGoal;
        if (levelGoal == null) return true; // can't determine side — don't over-restrict

        bool launcherIsOpponent = _launcher != null && _launcher != GameManager.Instance.GetLocalPlayer();
        List<Obstacle> ownSide = launcherIsOpponent
            ? levelGoal.ObstaclesToDestroy_Opponent
            : levelGoal.ObstaclesToDestroy_Player;

        return ownSide != null && ownSide.Contains(obstacle);
    }

    [SerializeField] float colorFillTimer = 1.33f;
    private bool isFilling = false;
    private IEnumerator AnimateArrowColors()
    {
        if (!isFilling)
        {
            isFilling = true;
            foreach (Image spriteFill in spriteFillList)
            {
                yield return new WaitForSeconds(colorFillTimer / 2);
                spriteFill.color = spriteTargetColor;
            }
            yield return new WaitForSeconds(colorFillTimer / 2);

            foreach (Image spriteFill in spriteFillList)
            {
                spriteFill.color = spriteStartColor;
            }
            isFilling = false;

        }

    }
    [SerializeField] private Transform launchDirectionTransform;
    
    [Command(defaultRouting = MessageTarget.Other)]
    public void CmdLaunchRocket()
    {
        if (_isLaunched) return;
        LaunchRocket();
    }

    [Button("Launch Rocket")]
    private void LaunchRocket()
    {
        _animator.enabled = false;
        _playerTrigger.enabled = false;
        spriteCanvas.SetActive(false);
        _obstacleDestructionTrigger.enabled = true;
        _fireParticleObject.SetActive(true);
        _smokeParticleObject.SetActive(true);
        StartCoroutine(MoveForwardWithAcceleration());
    }
    private IEnumerator MoveForwardWithAcceleration()
{
    _isLaunched = true;
    
    // Determine which direction to use based on the rotation
    // If rotated 90 on X, try using 'up' instead of 'forward'
    Vector3 launchDirection = launchDirectionTransform.up; // or .right depending on your setup
    
    while (_currentSpeed < maxSpeed)
    {
        _currentSpeed += acceleration * Time.deltaTime;
        transform.Translate(launchDirection * _currentSpeed * Time.deltaTime, Space.World);
        yield return null;
    }

    while (true)
    {
        transform.Translate(launchDirection * maxSpeed * Time.deltaTime, Space.World);
        yield return null;
    }
}
    void ApplyFakeGravity()
    {

        transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, 0, transform.position.z), 0.003f * 20);
    }

    public bool CheckForCollisions()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, 0.5f))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
