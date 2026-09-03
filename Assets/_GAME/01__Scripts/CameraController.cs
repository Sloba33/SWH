using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Linq;

public class CameraController : MonoBehaviour
{

    private int zoomCount = 7;
    private LevelGoal levelGoal;
    public Camera regularCamera;
    public Button rotateLeft, rotateRight, zoomIn, zoomOut;
    public TextMeshProUGUI zoomValue;
    public RectTransform floatingJoystick0, floatingJoystick1, floatingJoystick2, floatingJoystick3;
    public List<CinemachineFreeLook> cameras = new List<CinemachineFreeLook>();
    public List<GameObject> joysticks = new List<GameObject>();
    public int currentCamera = 0, prevCamera;
    public float desiredRotationAngle;
    public Camera mainCamera;
    public CinemachineBrain cmBrain;
    public CinemachineFreeLook vcMain;
    public RectTransform joystickHolder;
    public CinemachineFreeLook playerCamera;
    public CinemachineFreeLook.Orbit[] presetOrbits;
    public int[] presetZoomValues;
    public int currentZoom = 2;
    public int currentOrbit = 2;
    public PlayerController playerController;
    public float[] layerCullDistances = new float[32];

    private bool _isRotating = false;
    private Coroutine _rotationCoroutine;
    public float _initialRotation; // Store the initial rotation before each new rotation
    public float _previousRotation; // Store the previous rotation before each new rotation
    bool previousRotation;
    public Transform parentTransform;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            RotateLeft();
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            RotateRight();
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            ZoomOut();
            ZoomOut();
            ZoomOut();
        }
        if (Input.GetKeyDown(KeyCode.End))
        {
            ZoomIn();
            ZoomIn();
            ZoomIn();
        }
        if (!hasFallen && transform.position.y < fallThresholdY)
        {
            HandlePlayerFall();
        }
    }
    public Material MAT1;
    public Material MAT2;
    private Color originalMat1Color;
    private Color originalMat2Color;
    [SerializeField] private Renderer staircaseRenderer;
    // }
    private void OnEnable()
    {
        SetStartZoom();
        playerCamera.enabled = false;

    }
    private void Awake()
    {
        int j = 0;
        presetOrbits = new Cinemachine.CinemachineFreeLook.Orbit[zoomCount];
        presetZoomValues = new int[zoomCount];
        for (int i = 0; i < zoomCount; i++)
        {
            presetOrbits[i].m_Height = 2.4f + j * 0.8f;
            presetOrbits[i].m_Radius = 1.6f + j * 0.6f;
            presetZoomValues[i] = j * 25;
            j++;
        }
    }
    private void Start()
    {


        joystickHolder = FindObjectOfType<PlayerControls>().joystickHolder;
        levelGoal = FindFirstObjectByType<LevelGoal>(FindObjectsInactive.Include);
        // if (levelGoal != null && levelGoal.SpawnFallingObstacles)
        // {
        //     Debug.Log("Falling boxes so zooming out");
        //     yield return new WaitForSeconds(0.5f);
        //     ZoomIn();
        // }
        // yield return new WaitForSeconds(0.5f);
        // mat1Color = MAT1.color;
        // mat2Color = MAT2.color;

        vcam = FindObjectOfType<CinemachineVirtualCamera>();


        StartCoroutine(EnableCameraNextFrame());
        // StartCoroutine(FixZoomEndOfFrame());
        string sceneName = SceneManager.GetActiveScene().name;
        // if (!sceneName.ToLower().Contains("city"))
        // {
        //     Debug.Log("Disabling CameraController - not a city scene!");
        //     enabled = false;
        //     return;
        // }

        // Get the Cinemachine camera in the scene

        // mat1_color_illusive = new Color(mat1Color.r, mat1Color.g, mat1Color.b, 0);
        // mat2_color_illusive = new Color(mat2Color.r, mat2Color.g, mat2Color.b, 0);
        // if (!playerController.AI) { ZoomIn(); ZoomIn(); }    
        startYPosition = FindObjectOfType<Player>().transform.position.y;
    }
    private IEnumerator ZoomOutForFallingLevel()
    {
        yield return new WaitForSeconds(1f);
    }
    private void OnApplicationQuit()
    {
        if (playerController == null) return;
        if (!playerController.AI && MAT1 != null && MAT2 != null)
        {

            MAT1.color = mat1Color;
            MAT2.color = mat2Color;
        }
    }
    public void RotateRight()
    {
        if (_isRotating)
        {
            if (!previousRotation) return;
            _isCancellationPending = true; // Reset the cancellation flag
            if (_rotationCoroutine != null) StopCoroutine(_rotationCoroutine);
            _initialRotation = _previousRotation;
            _rotationCoroutine = StartCoroutine(RotateCameraLerp(false));
        }
        else
            _rotationCoroutine = StartCoroutine(RotateCameraLerp(false));
        previousRotation = false;

    }
    public float fadeDuration = 0.25f; // Adjust fade duration as needed
    public Color mat1Color, mat2Color, mat1_color_illusive, mat2_color_illusive;
    const string Illusive_Tag = "Illusive";
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Illusive_Tag))
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeMaterials(mat1_color_illusive, mat2_color_illusive));
        }
    }
    private Coroutine fadeCoroutine;

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Illusive_Tag))
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeMaterials(mat1Color, mat2Color));
        }
    }

    private IEnumerator FadeMaterials(Color targetMat1Color, Color targetMat2Color)
    {
        float startTime = Time.time;
        float endTime = startTime + fadeDuration;

        Color startMat1Color = MAT1.color;
        Color startMat2Color = MAT2.color;

        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / fadeDuration;

            MAT1.color = Color.Lerp(startMat1Color, targetMat1Color, t);
            MAT2.color = Color.Lerp(startMat2Color, targetMat2Color, t);

            yield return null;
        }

        MAT1.color = targetMat1Color;
        MAT2.color = targetMat2Color;

        fadeCoroutine = null; // Reset coroutine reference
    }

    public void RotateLeft()
    {
        if (_isRotating)
        {
            if (previousRotation) return;
            _isCancellationPending = true; // Reset the cancellation flag
            if (_rotationCoroutine != null) StopCoroutine(_rotationCoroutine);
            _initialRotation = _previousRotation;
            _rotationCoroutine = StartCoroutine(RotateCameraLerp(true));
        }
        else
            _rotationCoroutine = StartCoroutine(RotateCameraLerp(true));
        previousRotation = true;
    }
    public bool _isCancellationPending;
    public float currentRotation, targetValue, startValue, rotationDifference;
    IEnumerator RotateCameraLerp(bool isLeft)
    {
        if (isLeft) Debug.Log("Rotating left");
        else Debug.Log("Rotating right");
        _isRotating = true;
        Debug.Log("Player camera : " + playerCamera);
        currentRotation = playerCamera.m_XAxis.Value;
        Debug.Log("Current rotation :     " + currentRotation);
        startValue = currentRotation; // Use previous rotation as the starting point
        float duration;
        if (!_isCancellationPending)
        {
            targetValue = isLeft ? startValue - 90 : startValue + 90;
            Debug.Log($"Regular Target Rotation: {targetValue}"); // Debug statement to print the target rotation
            duration = 1.0f;
        }
        else
        {
            Debug.Log("Canceled Current rotation " + currentRotation);
            float newTargetValue;
            newTargetValue = isLeft ? targetValue - 90 : targetValue + 90;
            startValue = currentRotation;
            float rotationDifference = Mathf.Abs(newTargetValue - currentRotation);

            // Calculate the percentage of the rotation difference relative to the full rotation amount
            float rotationPercentage = rotationDifference / 90.0f;

            // Use the percentage to determine the proportional duration
            duration = 1.0f * rotationPercentage;
            // Debug.Log($" Rotation Diff: {rotationDifference}"); // Debug statement to print the target rotation
            Debug.Log("Canceled Expected rotation : " + newTargetValue);
            targetValue = newTargetValue;
            Debug.Log($"Canceled Target Rotation: {targetValue}"); // Debug statement to print the target rotation
            Debug.Log("Rotation Canceled");
            _isCancellationPending = false;
        }

        // float duration = 1.0f;
        float startTime = Time.time;
        float endTime = startTime + duration;

        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / duration;
            float newValue = Mathf.Lerp(startValue, targetValue, t);

            playerCamera.m_XAxis.Value = newValue;
            joystickHolder.localRotation = Quaternion.Euler(0, 0, newValue);

            yield return null;
        }

        _isRotating = false;
        _previousRotation = targetValue; // Update previous rotation for the next rotation
        _isCancellationPending = false;
        Debug.Log("Rotation Completed");
    }
    public float zoomDuration = 1f;
    public bool zoomAnimation;
    public void ZoomIn()
    {
        zoomOut.interactable = true;
        if (currentOrbit < presetOrbits.Length - 1)
        {
            currentZoom++;
            currentOrbit++;
            // if (zoomAnimation)
            AnimateOrbitChange(presetOrbits[currentOrbit].m_Height, presetOrbits[currentOrbit].m_Radius);
            // else
            // {
            //     playerCamera.m_Orbits[1].m_Height = presetOrbits[currentOrbit].m_Height;
            //     playerCamera.m_Orbits[1].m_Radius = presetOrbits[currentOrbit].m_Radius;
            // }
            zoomValue.text = presetZoomValues[currentZoom].ToString() + "%";
        }
        if (currentOrbit >= presetOrbits.Length - 1)
        {
            zoomIn.interactable = false;
        }
    }

    // private IEnumerator FixZoomEndOfFrame()
    // {
    //     yield return new WaitForEndOfFrame();  // Lets Cinemachine complete its first pass

    //     // Re-apply the snap as a "force refresh" (harmless if already correct)
    //     playerCamera.m_YAxis.Value = (float)currentOrbit / (zoomCount - 1f);
    // }
    public void SetStartZoom()
    {
        currentZoom = GameManager.Instance.defaultZoomValue;
        currentOrbit = currentZoom;


        playerCamera.m_Orbits[1].m_Height = presetOrbits[currentOrbit].m_Height;

        playerCamera.m_Orbits[1].m_Radius = presetOrbits[currentOrbit].m_Radius;


        playerCamera.m_YAxis.Value = 0.5f;

        if (zoomValue != null)
            zoomValue.text = presetZoomValues[currentZoom].ToString() + "%";

        if (currentOrbit < 1)
        {
            zoomOut.interactable = false;
        }
    }
    private IEnumerator EnableCameraNextFrame()
    {
        yield return null; // Wait one frame to ensure Cinemachine processes orbits
        playerCamera.enabled = true; // Now safe to go Live
        Debug.Log($"Camera enabled with orbit {currentOrbit}, Y-Value: {playerCamera.m_YAxis.Value}");
    }
    public void ZoomOut()
    {
        Debug.Log("Zooming Out");
        if (zoomIn != null) zoomIn.interactable = true;
        if (currentOrbit > 0)
        {
            currentZoom--;
            currentOrbit--;
            // if (zoomAnimation)
            AnimateOrbitChange(presetOrbits[currentOrbit].m_Height, presetOrbits[currentOrbit].m_Radius);
            // else
            // {
            //     playerCamera.m_Orbits[1].m_Height = presetOrbits[currentOrbit].m_Height;
            //     playerCamera.m_Orbits[1].m_Radius = presetOrbits[currentOrbit].m_Radius;
            // }
            zoomValue.text = presetZoomValues[currentZoom].ToString() + "%";
        }
        if (currentOrbit < 1)
        {
            zoomOut.interactable = false;
        }
    }
    private void AnimateOrbitChange(float targetHeight, float targetRadius)
    {
        // Animate Height
        DOTween.To(() => playerCamera.m_Orbits[1].m_Height, x => playerCamera.m_Orbits[1].m_Height = x, targetHeight, zoomDuration).SetEase(Ease.InOutQuad).Play();

        // Animate Radius
        DOTween.To(() => playerCamera.m_Orbits[1].m_Radius, x => playerCamera.m_Orbits[1].m_Radius = x, targetRadius, zoomDuration).SetEase(Ease.InOutQuad).Play();
    }

    public void ChangeCameraType()
    {
        mainCamera.orthographic = !mainCamera.orthographic;
    }
    public float fallThresholdY = -1.5f;
    private bool hasFallen = false;
    private float startYPosition;

    private CinemachineVirtualCamera vcam;



    private void HandlePlayerFall()
    {
        if (hasFallen) return;
        hasFallen = true;

        // CRITICAL: Create target at player's CURRENT position
        GameObject fallCamTarget = new GameObject("FallCamTarget");
        fallCamTarget.transform.position = transform.position; // NOT transform.position + offset

        if (vcMain != null)
        {
            // Immediately switch camera
            vcMain.Follow = fallCamTarget.transform;
            vcMain.LookAt = fallCamTarget.transform;
            vcMain.Priority = 100; // Force active
        }

        // Disable player controls immediately
        playerController = FindObjectOfType<PlayerController>();
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();

        if (playerController != null)
        {
            playerController.enabled = false;

            Player player = playerController.GetComponent<Player>();
            if (player != null)
            {
                StartCoroutine(player.LoseLevel(1.25f));
            }
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        Debug.Log("💀 Player fell - camera locked instantly!");
    }
}
