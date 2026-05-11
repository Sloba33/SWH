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
    public SphereCollider _playerTrigger;
    public CapsuleCollider _obstacleDestructionTrigger;
    private Rigidbody _rb;
    private Animator _animator;
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
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _coherenceSync = GetComponent<CoherenceSync>();

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

            if (_coherenceSync != null)
            {
                CoherenceSync obstacleSync = obstacle.GetComponent<CoherenceSync>();
                if (obstacleSync == null || !obstacleSync.HasStateAuthority) return;
            }

            obstacle.ParticleDestroy(Obstacle.ObstacleDestructionSource.Weapon);
        }
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
        while (_currentSpeed < maxSpeed)
        {
            _currentSpeed += acceleration * Time.deltaTime;
            transform.Translate(Vector3.forward * _currentSpeed * Time.deltaTime);
            yield return null;
        }


        while (true)
        {
            transform.Translate(Vector3.forward * maxSpeed * Time.deltaTime);
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
