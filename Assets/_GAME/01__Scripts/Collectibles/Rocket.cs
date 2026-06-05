using System;
using System.Collections;
using System.Collections.Generic;
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
        // _animator = GetComponent<Animator>();

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
        if (other.CompareTag(PlayerTag))
        {

            LaunchRocket();
            Debug.Log("Rocket Launch DETECTED");

        }
        if (other.CompareTag(rocketDisablerTag))
        {

            isDisabledForMultiplayer = true;
            Debug.Log("Rocket Disabled");

        }
        if (other.CompareTag(ObstacleTag) && _isLaunched && !isDisabledForMultiplayer)
        {
            if (other.GetComponent<Obstacle>() != null)
            {
                other.GetComponent<Obstacle>().ParticleDestroy(Obstacle.ObstacleDestructionSource.Weapon);
            }
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
    [SerializeField] private Transform launchDirectionTransform;
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
