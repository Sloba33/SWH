using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cinemachine;
using DG.Tweening;
public class Bomb : MonoBehaviour
{
    public AudioClip spawnSound;
    public bool isColored;
    public List<ObstacleColor> bombColors = new List<ObstacleColor>();
    public ObstacleColor bombColor;
    public Transform parent;
    public Player player;
    public AudioSource audioSource;
    public Rigidbody _rb;
    public GameObject playerCamera;
    public TextMeshPro countdownText;
    public GameObject textGameObject;
    public BoxCollider boxCollider;
    public float time = 3f;
    public ParticleSystem explosionParticle;
    [SerializeField] private MeshRenderer mesh;

    bool Grounded;
    private void Start()
    {
        IgnorePlayerCollision(FindObjectOfType<PlayerController>().GetComponent<Collider>());
        if (!isColored)
        {
            Invoke("Explode", time);
            StartCoroutine(Countdown());
        }
        else
        {
            boxCollider.enabled = false;
            StartCoroutine(ExplodeColored());
        }
    }
    public Collider[] _boxCollider = new Collider[1];
    public bool CheckForCollisions()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        // Check if the ray hits something within the specified distance
        if (Physics.Raycast(ray, 0.3f))
        {
            // The object is grounded
            Debug.Log("Grounded");
            return true;
        }
        else
        {
            // The object is not grounded
            Debug.Log("Not Grounded");
            return false;
        }
    }
    public void OnDestroy()
    {

    }
    private bool hasDetonated = false;
    public SphereCollider SphereShakeCollider;
    void CameraShake()
    {
        if (hasDetonated) return;

        hasDetonated = true;

        // Check if the player is within the trigger collider
        Debug.Log("SPhere radius " + SphereShakeCollider.radius);
        Collider[] colliders = Physics.OverlapSphere(transform.position, SphereShakeCollider.radius);

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                Debug.Log("Player found" + collider.name);
                // Run the desired function on the player
                PlayerController playerController = collider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    CinemachineFreeLook cmfl = FindObjectOfType<CinemachineFreeLook>();
                    if (cmfl != null)
                    {

                        CinemachineBasicMultiChannelPerlin noise = cmfl.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();

                        if (noise == null)
                        {
                            Debug.LogError("No Basic Multi Channel Perlin noise component found on the FreeLook camera!");
                            return;
                        }
                        Debug.Log("SHAKING CAMERA");
                        noise.m_AmplitudeGain = 1f;

                        // Animate: 0 → 1 over 0.5s, then 1 → 0 over 0.5s (total 1 second)
                        // You can tweak duration/intensity here
                        DOTween.To(() => noise.m_AmplitudeGain,
                                   x => noise.m_AmplitudeGain = x,
                                   1.5f, 0.5f) // Rise to full strength
                               .SetEase(Ease.OutQuad).Play() // Smooth start, quick peak
                               .OnComplete(() =>
                               {
                                   DOTween.To(() => noise.m_AmplitudeGain,
                                              x => noise.m_AmplitudeGain = x,
                                              0f, 0.5f) // Fade back to zero
                                          .SetEase(Ease.InQuad).Play();
                               });
                    }
                }
            }
        }

    }

    void ApplyFakeGravity()
    {

        transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, 0, transform.position.z), 0.003f * 20);
    }
    private void Update()
    {
        Grounded = CheckForCollisions();
        if (!Grounded) ApplyFakeGravity();
    }
    private void LateUpdate()
    {

        if (playerCamera != null)
            textGameObject.transform.LookAt(playerCamera.transform);
    }
    public IEnumerator Countdown()
    {
        countdownText.text = time + "...";
        time -= 1f;
        yield return new WaitForSeconds(1f);
        countdownText.text = time + "...";
        time -= 1f;
        yield return new WaitForSeconds(1f);
        countdownText.text = time + "...";
        time -= 1f;
    }
    public List<Obstacle> obstaclesToHit = new();
    public bool playerInsideCollider;
    public Collider playerCollider;
    private void OnTriggerEnter(Collider other)
    {

        Obstacle obstacle = other.GetComponent<Obstacle>();
        if (obstacle != null)
        {
            if (bombColor == obstacle.obstacleColor || bombColor == ObstacleColor.Universal)
            {
                obstaclesToHit.Add(obstacle);
            }
        }
        else if (other.CompareTag("Player") && !isColored)
        {
            player = other.GetComponent<Player>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideCollider = false;
            if (playerCollider != null)
            {
                Physics.IgnoreCollision(boxCollider, playerCollider, false);
                playerCollider = null;
            }
            player = null;
        }

        Obstacle obstacle = other.GetComponent<Obstacle>();
        if (obstacle != null && obstaclesToHit.Contains(obstacle))
        {
            obstaclesToHit.Remove(obstacle);
        }
    }
    public void IgnorePlayerCollision(Collider playerCollider)
    {
        this.playerCollider = playerCollider;
        Physics.IgnoreCollision(boxCollider, playerCollider, true);
        playerInsideCollider = true;
    }
    public void Explode()
    {
        LevelGoal levelGoal = FindObjectOfType<LevelGoal>();
        if (levelGoal != null)
        {
            for (int i = 0; i < obstaclesToHit.Count; i++)
            {
                if (obstaclesToHit[i] != null)
                {
                    Debug.Log("Obstacle found in levelgoal");
                    levelGoal.RemoveObstacle(obstaclesToHit[i]);
                    obstaclesToHit[i].ParticleDestroy(Obstacle.ObstacleDestructionSource.Bomb);
                }
            }
        }
        else
        {
            // countdownText.text = time + "...";
            for (int i = 0; i < obstaclesToHit.Count; i++)
            {
                if (obstaclesToHit[i] != null)
                    obstaclesToHit[i].ParticleDestroy(Obstacle.ObstacleDestructionSource.Bomb);
            }
        }
        if (player != null && !isColored) player.Die();
        explosionParticle.Play();
        textGameObject.SetActive(false);
        mesh.enabled = false;
        audioSource.Play();
        CameraShake();
        Destroy(gameObject, 0.75f);
    }
    public IEnumerator ExplodeColored()
    {
        Debug.Log("Exploding colored bomb");

        Obstacle[] obstacles = FindObjectsOfType<Obstacle>();
        List<Obstacle> obstaclesToNuke = new List<Obstacle>();
        foreach (Obstacle obstacle in obstacles)
        {
            Debug.Log("Obstacle Color : " + obstacle.obstacleColor + ", Bomb Color: " + bombColor);
            if (obstacle.obstacleColor == bombColor || bombColor == ObstacleColor.Universal)
            {
                Debug.Log("Matching obstacle found: " + obstacle.name);
                obstaclesToNuke.Add(obstacle);
            }
        }

        LevelGoal levelGoal = FindObjectOfType<LevelGoal>();
        yield return new WaitForSeconds(0.1f);

        foreach (Obstacle obs in obstaclesToNuke)
        {
            if (obs != null)
            {
                Debug.Log("Destroying obstacle: " + obs.name);
                if (levelGoal != null)
                    levelGoal.RemoveObstacle(obs);

                obs.ParticleDestroy(Obstacle.ObstacleDestructionSource.Bomb);
            }
        }

        textGameObject.SetActive(false);
        mesh.enabled = false;
        audioSource.Play();
        Destroy(gameObject, 0.15f);
    }
}
