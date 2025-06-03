using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TutorialMenuManager : MonoBehaviour
{
    public enum MenuTutorialStep
    {
        None,
        WorkButton,
        TrophyRoadButton,
        TrophyRoadReward,
        TrophyRoadBack,
        WorkersButton,
        UpgradeWorkerButton,
        WorkersScreenBack,
        WorkButton_End,
        Completed
    }

    public MenuTutorialStep currentTutorialStep = MenuTutorialStep.None;

    public List<TutorialStepData> tutorialStepsData;

    [System.Serializable]
    public class TutorialStepData
    {
        public MenuTutorialStep step;
        public GameObject hintGameObject;
        public GameObject buttonGameObject;
        public GameObject dynamicTargetContainer;
        public string dynamicTargetButtonName;
        public bool requiresSpecificAction = false;

        [System.NonSerialized] public Button actualButtonComponent;
    }

    public GameObject uiBlockerPanel;

    public List<GameObject> excludeButtonGameObjects;
    public List<GameObject> includeButtonGameObjects;

    private HashSet<GameObject> allManagedButtonGameObjects = new HashSet<GameObject>();
    private HashSet<GameObject> tutorialStepTargetButtonGameObjects = new HashSet<GameObject>();
    private HashSet<GameObject> activePersistentButtonGameObjects = new HashSet<GameObject>();

    private Dictionary<MenuTutorialStep, Button> stepToActualButtonComponent = new Dictionary<MenuTutorialStep, Button>();

    public event Action OnWorkButtonClickedDuringTutorial;
    public event Action<int, TrophyRewardType> OnTrophyRoadRewardClaimedDuringTutorial;

    private const string PREF_GAMEPLAY_TUTORIAL_COMPLETED = "GameplayTutorialCompleted";
    private const string PREF_INTRO_MENU_TUTORIAL_STAGE = "IntroMenuTutorialStage";
    private const string PREF_CURRENT_INTRO_LEVEL = "Level";

    private void Awake()
    {
    }

    private void Start()
    {
        foreach (var data in tutorialStepsData)
        {
            if (data.buttonGameObject != null)
            {
                Button btn = data.buttonGameObject.GetComponent<Button>();
                if (btn != null)
                {
                    data.actualButtonComponent = btn;
                    stepToActualButtonComponent[data.step] = btn;
                    tutorialStepTargetButtonGameObjects.Add(data.buttonGameObject);
                }
            }
        }

        PopulateAllManagedButtonGameObjects();

        foreach (GameObject go in allManagedButtonGameObjects)
        {
            if (go != null) go.SetActive(false);
        }
        DeactivateAllHints();

        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(false);

        InitializeTutorialState();
    }

    private void PopulateAllManagedButtonGameObjects()
    {
        allManagedButtonGameObjects = FindObjectsOfType<Button>(true)
                                      .Select(b => b.gameObject)
                                      .ToHashSet();

        foreach (GameObject go in includeButtonGameObjects)
        {
            if (go != null)
            {
                allManagedButtonGameObjects.Add(go);
            }
        }

        foreach (GameObject go in excludeButtonGameObjects)
        {
            if (go != null)
            {
                allManagedButtonGameObjects.Remove(go);
            }
        }

        foreach (GameObject go in tutorialStepTargetButtonGameObjects)
        {
            if (go != null)
            {
                allManagedButtonGameObjects.Add(go);
            }
        }
    }

    private void InitializeTutorialState()
    {
        int gameplayTutorialStatus = PlayerPrefs.GetInt(PREF_GAMEPLAY_TUTORIAL_COMPLETED, 0);
        int menuTutorialStage = PlayerPrefs.GetInt(PREF_INTRO_MENU_TUTORIAL_STAGE, 0);

        if (gameplayTutorialStatus == 1 && menuTutorialStage < (int)MenuTutorialStep.Completed)
        {
            if (uiBlockerPanel != null) uiBlockerPanel.SetActive(true);

            if (menuTutorialStage == (int)MenuTutorialStep.None)
            {
                currentTutorialStep = MenuTutorialStep.WorkButton;
                PlayerPrefs.SetInt(PREF_INTRO_MENU_TUTORIAL_STAGE, (int)MenuTutorialStep.WorkButton);
            }
            else
            {
                currentTutorialStep = (MenuTutorialStep)menuTutorialStage;
            }
            UpdateButtonAndHintVisibility();
        }
        else
        {
            currentTutorialStep = MenuTutorialStep.Completed;
            EndMenuTutorial();
        }
    }

    private void UpdateButtonAndHintVisibility()
    {
        RemoveAllTutorialButtonListeners();
        DeactivateAllHints();

        foreach (GameObject go in allManagedButtonGameObjects)
        {
            if (go != null && !activePersistentButtonGameObjects.Contains(go))
            {
                go.SetActive(false);
            }
        }

        TutorialStepData currentStepData = GetTutorialStepData(currentTutorialStep);
        if (currentStepData != null)
        {
            if (currentStepData.buttonGameObject == null && currentStepData.dynamicTargetContainer != null && !string.IsNullOrEmpty(currentStepData.dynamicTargetButtonName))
            {
                FindAndAssignDynamicButtonGameObject(currentStepData);
            }

            if (currentStepData.buttonGameObject != null)
            {
                currentStepData.buttonGameObject.SetActive(true);
                activePersistentButtonGameObjects.Add(currentStepData.buttonGameObject);
            }
            else
            {
            }

            if (currentStepData.hintGameObject != null)
            {
                currentStepData.hintGameObject.SetActive(true);
            }

            if (currentStepData.actualButtonComponent != null)
            {
                AddTutorialButtonListener(currentStepData.actualButtonComponent, currentStepData.step.ToString());
            }
        }
        else
        {
        }

        foreach (GameObject go in activePersistentButtonGameObjects)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }
    }

    private void AddTutorialButtonListener(Button button, string stepName)
    {
        if (button != null)
        {
            // button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnTutorialButtonClicked(stepName));
            button.interactable = true;
        }
    }

    private void RemoveAllTutorialButtonListeners()
    {
        foreach (var pair in stepToActualButtonComponent)
        {
            if (pair.Value != null)
            {
                pair.Value.onClick.RemoveAllListeners();
            }
        }
        foreach (var data in tutorialStepsData)
        {
            if (data.actualButtonComponent != null)
            {
                data.actualButtonComponent.onClick.RemoveAllListeners();
            }
        }
    }

    public void NotifyDynamicButtonsSpawned(MenuTutorialStep step)
    {
        if (currentTutorialStep == step)
        {
            UpdateButtonAndHintVisibility();
        }
    }

    public void FindAndAssignDynamicButtonGameObject(TutorialStepData data)
    {
        if (data.dynamicTargetContainer == null)
        {
            return;
        }

        GameObject foundGameObject = null;
        Button foundButtonComponent = null;

        Transform buttonTransform = data.dynamicTargetContainer.transform.Find(data.dynamicTargetButtonName);
        if (buttonTransform != null)
        {
            foundGameObject = buttonTransform.gameObject;
            foundButtonComponent = foundGameObject.GetComponent<Button>();
        }

        if (foundGameObject == null || foundButtonComponent == null)
        {
            foreach (Transform child in data.dynamicTargetContainer.transform)
            {
                Button potentialButton = child.GetComponent<Button>();
                if (potentialButton != null && potentialButton.gameObject.activeInHierarchy)
                {
                    foundGameObject = child.gameObject;
                    foundButtonComponent = potentialButton;
                    break;
                }
            }
        }

        if (foundGameObject != null && foundButtonComponent != null)
        {
            data.buttonGameObject = foundGameObject;
            data.actualButtonComponent = foundButtonComponent;

            tutorialStepTargetButtonGameObjects.Add(foundGameObject);
            if (!stepToActualButtonComponent.ContainsKey(data.step))
            {
                stepToActualButtonComponent[data.step] = foundButtonComponent;
            }
            allManagedButtonGameObjects.Add(foundGameObject);
        }
        else
        {
        }
    }

    private void DeactivateAllHints()
    {
        foreach (var data in tutorialStepsData)
        {
            if (data.hintGameObject != null)
            {
                data.hintGameObject.SetActive(false);
            }
        }
    }

    public void OnTutorialButtonClicked(string clickedStepName)
    {
        TutorialStepData currentStepData = GetTutorialStepData(currentTutorialStep);

        if (currentTutorialStep == MenuTutorialStep.Completed || currentTutorialStep == MenuTutorialStep.None)
        {
            return;
        }

        if (currentStepData == null || currentStepData.step.ToString() != clickedStepName)
        {
            return;
        }

        if (currentStepData.requiresSpecificAction)
        {
            return;
        }

        if (clickedStepName == MenuTutorialStep.WorkButton.ToString())
        {
            OnWorkButtonClickedDuringTutorial?.Invoke();
            // REMOVED: return; // This was the bug: it prevented ProgressToNextStep() from being called.
            // We now progress the tutorial state immediately, even if levels are loaded externally.
        }
        else if (clickedStepName == MenuTutorialStep.TrophyRoadButton.ToString())
        {
        }
        else if (clickedStepName == MenuTutorialStep.TrophyRoadBack.ToString())
        {
        }
        else if (clickedStepName == MenuTutorialStep.WorkersButton.ToString())
        {
        }
        else if (clickedStepName == MenuTutorialStep.WorkersScreenBack.ToString())
        {
        }
        else if (clickedStepName == MenuTutorialStep.WorkButton_End.ToString())
        {
        }

        ProgressToNextStep(); // This now always gets called, progressing PREF_INTRO_MENU_TUTORIAL_STAGE
    }

    public void OnSpecificActionCompleted(string actionStepName, int trophyRequirement = 0, TrophyRewardType rewardType = TrophyRewardType.Coins_Small)
    {
        TutorialStepData currentStepData = GetTutorialStepData(currentTutorialStep);

        if (currentStepData == null || currentStepData.step.ToString() != actionStepName || !currentStepData.requiresSpecificAction)
        {
            return;
        }

        if (actionStepName == MenuTutorialStep.TrophyRoadReward.ToString())
        {
            OnTrophyRoadRewardClaimedDuringTutorial?.Invoke(trophyRequirement, rewardType);
        }

        ProgressToNextStep();
    }

    // This method is no longer used by LevelGoal to trigger progression
    // as progression happens via ProgressToNextStep() directly.
    // However, it could be used if you need a specific external trigger to advance
    // after a level (not the WorkButton click itself).
    public void ContinueTutorialAfterIntroLevels()
    {
        // This method was originally intended to be called by LevelGoal,
        // but with the fix to OnTutorialButtonClicked for WorkButton,
        // this method is now redundant for its original purpose.
        // If you had other "external" level-completion steps, you might use it.
        // For now, it's safe to keep it, but it won't be explicitly called for WorkButton progression.
    }

    private void ProgressToNextStep()
    {
        MenuTutorialStep nextStep = GetNextStep(currentTutorialStep);
        currentTutorialStep = nextStep;
        PlayerPrefs.SetInt(PREF_INTRO_MENU_TUTORIAL_STAGE, (int)currentTutorialStep);


        if (currentTutorialStep == MenuTutorialStep.Completed)
        {
            EndMenuTutorial();
        }
        else
        {
            UpdateButtonAndHintVisibility();
        }
    }

    public TutorialStepData GetTutorialStepData(MenuTutorialStep step)
    {
        foreach (var data in tutorialStepsData)
        {
            if (data.step == step)
            {
                return data;
            }
        }
        return null;
    }

    private MenuTutorialStep GetNextStep(MenuTutorialStep current)
    {
        switch (current)
        {
            case MenuTutorialStep.WorkButton: return MenuTutorialStep.TrophyRoadButton;
            case MenuTutorialStep.TrophyRoadButton: return MenuTutorialStep.TrophyRoadReward;
            case MenuTutorialStep.TrophyRoadReward: return MenuTutorialStep.TrophyRoadBack;
            case MenuTutorialStep.TrophyRoadBack: return MenuTutorialStep.WorkersButton;
            case MenuTutorialStep.WorkersButton: return MenuTutorialStep.UpgradeWorkerButton;
            case MenuTutorialStep.UpgradeWorkerButton: return MenuTutorialStep.WorkersScreenBack;
            case MenuTutorialStep.WorkersScreenBack: return MenuTutorialStep.WorkButton_End;
            case MenuTutorialStep.WorkButton_End: return MenuTutorialStep.Completed;
            default: return MenuTutorialStep.Completed;
        }
    }

    public void EndMenuTutorial()
    {
        PlayerPrefs.SetInt(PREF_INTRO_MENU_TUTORIAL_STAGE, (int)MenuTutorialStep.Completed);
        currentTutorialStep = MenuTutorialStep.Completed;

        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(false);

        SetAllManagedButtonsActive(true);
        RestoreAllButtonColorsToNormal();
        RemoveAllTutorialButtonListeners();
        DeactivateAllHints();
        activePersistentButtonGameObjects.Clear();
        tutorialStepTargetButtonGameObjects.Clear();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.NotifyTutorialManagerCompleted();
        }

        // Destroy(gameObject); // Uncomment if you want to destroy the TutorialMenuManager GameObject
    }

    private void SetAllManagedButtonsActive(bool active)
    {
        foreach (GameObject go in allManagedButtonGameObjects)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }

    private void RestoreAllButtonColorsToNormal()
    {
        foreach (GameObject go in allManagedButtonGameObjects)
        {
            if (go != null)
            {
                Button btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = true;
                }
            }
        }
    }

    private void OnDestroy()
    {
        OnWorkButtonClickedDuringTutorial = null;
        OnTrophyRoadRewardClaimedDuringTutorial = null;
    }
}