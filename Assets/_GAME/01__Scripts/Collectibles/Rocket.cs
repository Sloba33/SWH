using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
public class Rocket : MonoBehaviour
{
    public SphereCollider _playerTrigger;
    public CapsuleCollider _obstacleDestructionTrigger;
    private Rigidbody _rb;
    private Animator _animator;
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
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

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
            // Assuming there's a method to handle rocket collection
            LaunchRocket();
            Debug.Log("Rocket Launch DETECTED");

        }
        if (other.CompareTag(ObstacleTag) && _isLaunched)
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
            // yield return new WaitForSeconds(colorFillTimer);
            foreach (Image spriteFill in spriteFillList)
            {
                spriteFill.color = spriteStartColor;
            }
            isFilling = false;

        }

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

        // Maintain full speed after reaching it
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

        // Check if the ray hits something within the specified distance
        if (Physics.Raycast(ray, 0.5f))
        {
            // The object is grounded
            // Debug.Log("Grounded");
            return true;
        }
        else
        {
            // The object is not grounded
            // Debug.Log("Not Grounded");
            return false;
        }
    }
}
