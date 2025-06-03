using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public GameObject loadingScreen;
    public Image loadingBarFill;
    public int currentLevelIndex;
    public int nextLevelIndex;
    public bool isLoadingScene;

    [Header("Loading Settings")]
    [SerializeField] private float minimumLoadingTime = 2.0f; // Minimum time loading screen is visible
    [SerializeField] private bool allowSceneActivationOnProgress = true; // Set to false for manual activation

    private void Awake()
    {
        // Optional: Make this a Singleton if you want it to persist and be easily accessible
        // Example:
        // if (Instance != null && Instance != this)
        // {
        //     Destroy(gameObject);
        //     return;
        // }
        // Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Subscribe to the sceneLoaded event to handle disabling the loading screen
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Debug.Log("Loading scene");
        currentLevelIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentLevelIndex == 0) isLoadingScene = true;
        else isLoadingScene = false;

        // Correctly calculate nextLevelIndex and handle boundary
        nextLevelIndex = currentLevelIndex + 1;
        if (nextLevelIndex >= SceneManager.sceneCountInBuildSettings)
        {
            nextLevelIndex = currentLevelIndex; // Or handle end-of-game logic
            Debug.LogWarning("Attempted to load next level beyond build settings. Looping to current level or custom end logic.");
        }

        int firstTime = PlayerPrefs.GetInt("FirstTime", 0);
        if (isLoadingScene && firstTime != 0)
        {
            Debug.Log("Its a loading scene, so we load main menu.");
            StartCoroutine(LoadLevelInternal(1)); // Use the internal loading method
        }
        else if (firstTime == 0 && isLoadingScene)
        {
            Debug.Log("First time launch, loading onboarding/tutorial scene.");
            // Important: Remember to set PlayerPrefs.SetInt("FirstTime", 1);
            // in your onboarding/tutorial scene (scene 2) once it's complete.
            StartCoroutine(LoadLevelInternal(2)); // Use the internal loading method
        }
    }

    public void LoadNextJob()
    {
        // Ensure PlayerPrefs.GetInt("Level") is managed correctly to avoid out-of-bounds
        StartCoroutine(LoadLevelInternal(PlayerPrefs.GetInt("Level") + 3));
    }

    // Refactored internal method for loading by index
    private IEnumerator LoadLevelInternal(int index)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(index);
        operation.allowSceneActivation = allowSceneActivationOnProgress; // Control activation

        loadingScreen.SetActive(true);
        float startTime = Time.time;

        // Wait until the scene is fully loaded (progress 0.9)
        while (operation.progress < 0.9f)
        {
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);
            loadingBarFill.fillAmount = progressValue;
            yield return null;
        }

        // Now that progress is 0.9, ensure minimum loading time is met
        float timeElapsed = Time.time - startTime;
        if (timeElapsed < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - timeElapsed);
        }

        // For older devices, if allowSceneActivationOnProgress was false, handle manual activation
        if (!operation.allowSceneActivation)
        {
            // The bar is full, minimum time met, now allow the scene to activate.
            // You could add a "Press Any Key" prompt here if desired.
            operation.allowSceneActivation = true;
            // Wait one more frame for the scene to become active
            yield return null;
        }

        // The scene is now active. The OnSceneLoaded event will handle disabling the loading screen.
        Debug.Log("Scene loading complete for index: " + index + ". Scene will now activate.");
    }

    // Refactored internal method for loading by name
    public void LoadLevelWithIndex(int index)
    {
        StartCoroutine(LoadLevelInternal(index));
    }
    private IEnumerator LoadSceneFileInternal(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = allowSceneActivationOnProgress;

        loadingScreen.SetActive(true);
        float startTime = Time.time;

        while (operation.progress < 0.9f)
        {
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);
            loadingBarFill.fillAmount = progressValue;
            yield return null;
        }

        float timeElapsed = Time.time - startTime;
        if (timeElapsed < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - timeElapsed);
        }

        if (!operation.allowSceneActivation)
        {
            operation.allowSceneActivation = true;
            yield return null;
        }

        Debug.Log("Scene loading complete for name: " + sceneName + ". Scene will now activate.");
    }

    // --- Public methods for external calls ---

    public void LoadScene()
    {
        StartCoroutine(LoadLevelInternal(nextLevelIndex));
    }

    public void LoadSceneFile(string sceneName) // Renamed from original to be consistent, but you can keep the original name
    {
        StartCoroutine(LoadSceneFileInternal(sceneName));
    }

    public void LoadSceneByName(string sceneName) // This now just calls the above
    {
        LoadSceneFile(sceneName);
    }

    public void LoadNextLevel()
    {
        StartCoroutine(LoadLevelInternal(nextLevelIndex));
    }

    public void ReloadScene()
    {
        StartCoroutine(LoadLevelInternal(currentLevelIndex));
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadLevelInternal(1));
    }

    public void LoadSpecificScene(int index)
    {
        StartCoroutine(LoadLevelInternal(index));
    }

    public void LeaveMainScene()
    {
        StartCoroutine(LoadLevelInternal(2));
    }

    // --- Event Handler for Scene Loaded ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // This method is called after a scene has finished loading.
        // We can safely deactivate the loading screen here, as the new scene is fully loaded and initialized.
        if (loadingScreen != null && loadingScreen.activeSelf)
        {
            loadingScreen.SetActive(false);
            Debug.Log("Loading screen deactivated after loading scene: " + scene.name);
        }
    }
}