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
    public event Action<int, TrophyRewardType> OnTrophyRoadRewardClaimedDuringTutorial; // Assuming TrophyRewardType is defined elsewhere

    private const string PREF_GAMEPLAY_TUTORIAL_COMPLETED = "GameplayTutorialCompleted";
    private const string PREF_INTRO_MENU_TUTORIAL_STAGE = "IntroMenuTutorialStage";

    private void Awake()
    {
        // Awake can remain empty or contain very basic initialization not related to scene objects
    }

    private void Start()
    {
        // Initialize stepToActualButtonComponent and populate tutorialStepTargetButtonGameObjects
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

        // Check the tutorial status early to decide behavior
        int gameplayTutorialStatus = PlayerPrefs.GetInt(PREF_GAMEPLAY_TUTORIAL_COMPLETED, 0);
        int menuTutorialStage = PlayerPrefs.GetInt(PREF_INTRO_MENU_TUTORIAL_STAGE, 0);
        bool tutorialIsActive = (gameplayTutorialStatus == 1 && menuTutorialStage < (int)MenuTutorialStep.Completed);

        if (tutorialIsActive)
        {
            // If the tutorial is active, initially hide all buttons that this manager will control
            foreach (GameObject go in allManagedButtonGameObjects)
            {
                if (go != null) go.SetActive(false);
            }
            if (uiBlockerPanel != null) uiBlockerPanel.SetActive(true); // Blocker panel is active during tutorial
        }
        else
        {
            // If the tutorial is completed (or not yet started/relevant),
            // ensure all buttons that *might* have been managed are active.
            // This prevents the tutorial from accidentally hiding them on start.
            SetAllManagedButtonsActive(true);
            // RestoreAllButtonColorsToNormal(); // REMOVED: This method is no longer used or is removed entirely to prevent unintended re-enabling of interactability.
            if (uiBlockerPanel != null) uiBlockerPanel.SetActive(false); // Blocker panel is off if tutorial is not active
        }

        DeactivateAllHints(); // Always deactivate hints on start, they are only shown during tutorial steps.

        // Initialize tutorial state, this will proceed with the tutorial flow
        // or mark it as completed if tutorialIsActive was false.
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
                if (go.GetComponent<Button>() != null)
                {
                    if (go.GetComponent<CharacterSelector>() != null && go.GetComponent<CharacterSelector>().characterType != CharacterType.Character_Standard)
                    {
                        if (PlayerPrefs.GetInt("Level", 0) < 3)
                            go.GetComponent<Button>().interactable = false; // Disable excluded buttons
                    }
                }
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
                Debug.Log("Setting intro menu tutorial stage to WorkButton");
                PlayerPrefs.SetInt(PREF_INTRO_MENU_TUTORIAL_STAGE, (int)MenuTutorialStep.WorkButton);
            }
            else
            {
                // When initializing, if the saved stage is an internal step, revert to its parent entry point
                switch ((MenuTutorialStep)menuTutorialStage)
                {
                    case MenuTutorialStep.TrophyRoadReward:
                    case MenuTutorialStep.TrophyRoadBack:
                        currentTutorialStep = MenuTutorialStep.TrophyRoadButton;
                        break;
                    case MenuTutorialStep.UpgradeWorkerButton:
                    case MenuTutorialStep.WorkersScreenBack:
                        currentTutorialStep = MenuTutorialStep.WorkersButton;
                        break;
                    default:
                        currentTutorialStep = (MenuTutorialStep)menuTutorialStage;
                        break;
                }
            }
            UpdateButtonAndHintVisibility();
        }
        else
        {
            currentTutorialStep = MenuTutorialStep.Completed;
            EndMenuTutorial(); // Calls EndMenuTutorial, which will now primarily perform cleanup
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
                // Debug.LogWarning($"Button GameObject not found for step: {currentTutorialStep}");
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
            // Debug.LogWarning($"TutorialStepData not found for currentTutorialStep: {currentTutorialStep}");
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
            button.onClick.AddListener(() => OnTutorialButtonClicked(stepName));
            button.interactable = true; // Ensure interactable when it's the current tutorial target
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
            // Debug.LogWarning($"Dynamic button '{data.dynamicTargetButtonName}' not found in container '{data.dynamicTargetContainer.name}' for step: {data.step}");
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

        ProgressToNextStep();
    }

    public void OnSpecificActionCompleted(string actionStepName, int trophyRequirement = 0, TrophyRewardType rewardType = TrophyRewardType.Coins_Small) // Assuming TrophyRewardType is defined elsewhere
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

    public void ContinueTutorialAfterIntroLevels()
    {
        // This method is now effectively redundant with the fix to OnTutorialButtonClicked for WorkButton
        // However, it can remain if you have other scenarios where an external trigger advances the tutorial.
    }

    private void ProgressToNextStep()
    {
        MenuTutorialStep previousStep = currentTutorialStep; // Capture current before updating
        MenuTutorialStep nextStep = GetNextStep(currentTutorialStep);
        currentTutorialStep = nextStep; // currentTutorialStep accurately tracks the *current* state

        // Determine the step to actually save for crash recovery
        MenuTutorialStep actualStepToSave = currentTutorialStep;
        switch (currentTutorialStep)
        {
            case MenuTutorialStep.TrophyRoadReward:
            case MenuTutorialStep.TrophyRoadBack:
                actualStepToSave = MenuTutorialStep.TrophyRoadButton; // Revert to parent entry point
                break;
            case MenuTutorialStep.UpgradeWorkerButton:
            case MenuTutorialStep.WorkersScreenBack:
                actualStepToSave = MenuTutorialStep.WorkersButton; // Revert to parent entry point
                break;
                // For other steps, actualStepToSave remains currentTutorialStep (e.g., WorkButton, TrophyRoadButton, WorkersButton, WorkButton_End, Completed)
        }
        Debug.Log("Setting intro menu tutorial stage to: " + actualStepToSave);
        PlayerPrefs.SetInt(PREF_INTRO_MENU_TUTORIAL_STAGE, (int)actualStepToSave);
        PlayerPrefs.Save(); // Explicitly save PlayerPrefs immediately for crash prevention

        // Generalized logic for disabling buttons after their sequence is complete
        switch (previousStep)
        {
            case MenuTutorialStep.TrophyRoadBack: // Completed Trophy Road sequence
                Button trophyRoadButton = GetTutorialStepData(MenuTutorialStep.TrophyRoadButton)?.actualButtonComponent;
                if (trophyRoadButton != null)
                {
                    // trophyRoadButton.interactable = false; // Interactability is now managed by other systems
                }
                break;
            case MenuTutorialStep.WorkersScreenBack: // Completed Workers sequence
                Button workersButton = GetTutorialStepData(MenuTutorialStep.WorkersButton)?.actualButtonComponent;
                if (workersButton != null)
                {
                    // workersButton.interactable = false; // Interactability is now managed by other systems
                }
                break;
                // Add more cases here for other sub-menu sequences if needed later
        }

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
        PlayerPrefs.Save(); // Ensure completion state is saved

        currentTutorialStep = MenuTutorialStep.Completed;

        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(false);

        // REMOVED these two lines as they were causing conflicts:
        // SetAllManagedButtonsActive(true);
        // RestoreAllButtonColorsToNormal();

        RemoveAllTutorialButtonListeners();
        DeactivateAllHints();
        activePersistentButtonGameObjects.Clear();
        tutorialStepTargetButtonGameObjects.Clear();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.NotifyTutorialManagerCompleted();
        }

        // Keep the GameObject in the scene as requested.
        // Destroy(gameObject); // Do NOT uncomment
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

    // This method is now likely unused or its functionality of setting interactable=true has been removed.
    // Consider removing it entirely if it's no longer needed.
    private void RestoreAllButtonColorsToNormal()
    {
        foreach (GameObject go in allManagedButtonGameObjects)
        {
            if (go != null)
            {
                Button btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    // This line was the main problem, removed to prevent unintended re-enabling.
                    // btn.interactable = true;
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