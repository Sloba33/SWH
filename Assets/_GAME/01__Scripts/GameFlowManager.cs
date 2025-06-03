// GameFlowManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public event Action OnWorkButtonClickedByTutorial;
    public event Action<int, TrophyRewardType> OnTrophyRoadRewardClaimedByTutorial;
    public event Action OnAnyLevelCompleted; 
    
    // --- CORRECTED: OnTutorialFullyComplete event is here ---
    public event Action OnTutorialFullyComplete; 

    private bool _introLevelsJustCompleted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "01_MainMenu") 
        {
            TutorialMenuManager tutorialManager = FindObjectOfType<TutorialMenuManager>();
            if (tutorialManager != null)
            {
                // Subscribe TutorialManager's internal events to GameFlowManager's public events
                // so GameFlowManager can re-broadcast them.
                tutorialManager.OnWorkButtonClickedDuringTutorial += HandleWorkButtonClickedByTutorial;
                tutorialManager.OnTrophyRoadRewardClaimedDuringTutorial += HandleTrophyRoadRewardClaimedByTutorial;

                // IMPORTANT: TutorialManager's Start() will call InitializeTutorialState()
                // which determines which stage of the tutorial to start.
                // No explicit call needed here from GameFlowManager.
                Debug.Log("TutorialManager found and initialized.");
            }
        }
    }

    private void HandleWorkButtonClickedByTutorial()
    {
        OnWorkButtonClickedByTutorial?.Invoke(); 
    }

    private void HandleTrophyRoadRewardClaimedByTutorial(int trophyReq, TrophyRewardType rewardType)
    {
        OnTrophyRoadRewardClaimedByTutorial?.Invoke(trophyReq, rewardType); 
    }

    // This is called by TutorialMenuManager when its sequence is done
    public void NotifyTutorialManagerCompleted()
    {
        OnTutorialFullyComplete?.Invoke();
    }


    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void NotifyIntroLevelsCompleted()
    {
        LoadScene("01_MainMenu"); 
    }
}