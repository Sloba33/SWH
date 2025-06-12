// LevelSelectionManager.cs (MODIFIED: UIShiny and UIEffectTweener control)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Coffee.UIEffects;

public class LevelSelectionManager : MonoBehaviour
{
    public bool useChapters = false;

    public GameObject chapterSelectionPanel;
    public RectTransform chapterContentTransform;
    
    public GameObject levelSelectionPanel;
    public RectTransform levelContentTransform;

    public Button backToChaptersButton;

    public RectTransform chapterButtonPrefab;
    public RectTransform levelButtonPrefab;
    public GameObject checkmarkPrefab;

    public List<ChapterDefinition> allChapterDefinitions;
    public List<Level> allLevels;

    private SceneLoader sceneLoader;

    private void Awake()
    {
        if (chapterSelectionPanel != null) chapterSelectionPanel.SetActive(false);
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        sceneLoader = FindObjectOfType<SceneLoader>();
        if (sceneLoader == null)
        {
            Debug.LogWarning("SceneLoader not found. Attempting to use GameFlowManager for scene loading if needed. Please ensure a 'SceneLoader' component exists if you use it for specific scene loading.");
        }

        if (backToChaptersButton != null)
        {
            backToChaptersButton.onClick.AddListener(OnBackToChapters);
        }

        InitializeLevelSelectionUI();
    }

    public void OpenLevelSelection()
    {
        ClearContentPanel(chapterContentTransform);
        ClearContentPanel(levelContentTransform);

        if (chapterSelectionPanel != null) chapterSelectionPanel.SetActive(false);
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(false);

        InitializeLevelSelectionUI();
    }

    private void InitializeLevelSelectionUI()
    {
        if (useChapters)
        {
            if (chapterSelectionPanel != null) chapterSelectionPanel.SetActive(true);
            DisplayChapters();
        }
        else
        {
            if (levelSelectionPanel != null) levelSelectionPanel.SetActive(true);
            DisplayAllLevels();
        }
    }

    private void ClearContentPanel(Transform contentParent)
    {
        if (contentParent == null)
        {
            Debug.LogError("LevelSelectionManager: Content parent transform is null! Cannot clear content.");
            return;
        }
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    private void DisplayChapters()
    {
        if (chapterContentTransform == null)
        {
            Debug.LogError("LevelSelectionManager: Chapter Content Transform is not assigned! Cannot display chapters.");
            return;
        }

        ClearContentPanel(chapterContentTransform);

        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(false);

        if (allChapterDefinitions == null || allChapterDefinitions.Count == 0)
        {
            Debug.LogWarning("LevelSelectionManager: No ChapterDefinition assets assigned. Please assign them in the Inspector if 'useChapters' is true.");
            return;
        }

        LayoutGroup layoutGroup = chapterContentTransform.GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        foreach (ChapterDefinition chapterDef in allChapterDefinitions)
        {
            if (chapterButtonPrefab == null)
            {
                Debug.LogError("LevelSelectionManager: Chapter Button Prefab is not assigned!");
                continue;
            }

            RectTransform chapterButtonGO = Instantiate(chapterButtonPrefab, chapterContentTransform);
            Chapter chapterMono = chapterButtonGO.GetComponent<Chapter>();
            Button buttonComponent = chapterButtonGO.GetComponent<Button>();
            chapterMono.chapterImage.sprite = chapterDef.chapterSprite;
            if (chapterMono != null)
            {
                chapterMono.SetChapterName(chapterDef.chapterName);
            }
            else
            {
                TextMeshProUGUI tmpText = chapterButtonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = chapterDef.chapterName;
                else Debug.LogWarning($"Chapter button prefab '{chapterButtonPrefab.name}' is missing a 'Chapter' script or a child TextMeshProUGUI component.");
            }

            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => OnChapterSelected(chapterDef));
            }
            else
            {
                Debug.LogWarning($"Chapter button prefab '{chapterButtonPrefab.name}' is missing a Button component.");
            }
        }

        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(chapterContentTransform);
        }
    }

    public void OnChapterSelected(ChapterDefinition selectedChapterDef)
    {
        if (CharacterManager.Instance != null) CharacterManager.Instance.PlayClick();

        if (chapterSelectionPanel != null) chapterSelectionPanel.SetActive(false);
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(true);
        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(true);

        DisplayLevelsForChapter(selectedChapterDef);
    }

    private void DisplayLevelsForChapter(ChapterDefinition chapterDef)
    {
        if (levelContentTransform == null)
        {
            Debug.LogError("LevelSelectionManager: Level Content Transform is not assigned! Cannot display levels.");
            return;
        }

        ClearContentPanel(levelContentTransform);

        if (chapterDef.levelsInChapter == null || chapterDef.levelsInChapter.Count == 0)
        {
            Debug.LogWarning($"Chapter '{chapterDef.chapterName}' has no Level assets assigned. No levels to display.");
            return;
        }

        // Sort levels by their levelNumber to ensure correct progression order
        chapterDef.levelsInChapter.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

        LayoutGroup layoutGroup = levelContentTransform.GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        int currentUnlockedLevel = PlayerPrefs.GetInt("Level", 0);

        foreach (Level level in chapterDef.levelsInChapter)
        {
            InstantiateLevelButton(level, currentUnlockedLevel, levelContentTransform);
        }

        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(levelContentTransform);
        }
    }

    private void DisplayAllLevels()
    {
        if (levelContentTransform == null)
        {
            Debug.LogError("LevelSelectionManager: Level Content Transform is not assigned! Cannot display all levels.");
            return;
        }

        ClearContentPanel(levelContentTransform);

        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(false);

        if (allLevels == null || allLevels.Count == 0)
        {
            Debug.LogWarning("LevelSelectionManager: No Level assets assigned to 'allLevels' list. Please assign them in the Inspector if 'useChapters' is false.");
            return;
        }

        // Sort all levels by their levelNumber to ensure correct progression order
        allLevels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

        LayoutGroup layoutGroup = levelContentTransform.GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        int currentUnlockedLevel = PlayerPrefs.GetInt("Level", 0);

        foreach (Level level in allLevels)
        {
            InstantiateLevelButton(level, currentUnlockedLevel, levelContentTransform);
        }

        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(levelContentTransform);
        }
    }

    private void InstantiateLevelButton(Level level, int currentUnlockedLevel, Transform contentParent)
    {
        if (levelButtonPrefab == null)
        {
            Debug.LogError("LevelSelectionManager: Level Button Prefab is not assigned!");
            return;
        }
        
        // Safety check for sceneBuildIndex
        if (level.sceneBuildIndex < 0 || level.sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"Level '{level.levelNumber}' (Scene Build Index: {level.sceneBuildIndex}) has an invalid sceneBuildIndex. Scene not found in Build Settings. Button will not be created.");
            return;
        }

        RectTransform levelButtonGO = Instantiate(levelButtonPrefab, contentParent);
        Button buttonComponent = levelButtonGO.GetComponent<Button>();

        TextMeshProUGUI tmpText = levelButtonGO.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = level.levelNumber.ToString();
        }
        else
        {
            Debug.LogWarning($"Level button prefab '{levelButtonPrefab.name}' is missing a child TextMeshProUGUI component.");
        }

        levelButtonGO.gameObject.name = "Level_" + level.levelNumber;

        if (buttonComponent != null)
        {
            // Set interactability: A level is unlocked if its levelNumber is less than or equal to
            // the currentUnlockedLevel (from PlayerPrefs) + 1.
            buttonComponent.interactable = level.levelNumber <= (currentUnlockedLevel + 1);

            // Control UIShiny and UIEffectTweener components
            // The "latest unlocked that wasn't completed yet" is the level with levelNumber == (currentUnlockedLevel + 1)
            bool isLatestUnbeatenLevel = (level.levelNumber == (currentUnlockedLevel + 1));

            UIShiny uiShiny = levelButtonGO.GetComponent<UIShiny>();
            UIEffectTweener uiEffectTweener = levelButtonGO.GetComponent<UIEffectTweener>();

            if (uiShiny != null)
            {
                uiShiny.enabled = isLatestUnbeatenLevel;
            }
            if (uiEffectTweener != null)
            {
                uiEffectTweener.enabled = isLatestUnbeatenLevel;
            }

            // Check if the level is completed: A level is completed if its levelNumber is less than or equal to
            // the currentUnlockedLevel (from PlayerPrefs).
            if (checkmarkPrefab != null && level.levelNumber <= currentUnlockedLevel)
            {
                GameObject checkmarkInstance = Instantiate(checkmarkPrefab, levelButtonGO.transform);
                RectTransform checkmarkRect = checkmarkInstance.GetComponent<RectTransform>();

                if (checkmarkRect != null)
                {
                    // Position the checkmark in the top-right corner of the button
                    checkmarkRect.anchorMin = new Vector2(1, 1);
                    checkmarkRect.anchorMax = new Vector2(1, 1);
                    checkmarkRect.pivot = new Vector2(1, 1);

                    // Adjust anchored position for a slight offset from the very corner
                    // You might need to tweak these values based on your UI design and checkmark size
                    checkmarkRect.anchoredPosition = new Vector2(-10, -10); 
                    // Optional: Set a specific size for the checkmark if it's not handled by its prefab
                    // checkmarkRect.sizeDelta = new Vector2(20, 20); 
                }
            }

            int sceneToLoadIndex = level.sceneBuildIndex; 
            buttonComponent.onClick.AddListener(() => OnLevelSelected(sceneToLoadIndex));
        }
        else
        {
            Debug.LogWarning($"Level button prefab '{levelButtonPrefab.name}' is missing a Button component.");
        }
    }

    private void OnLevelSelected(int sceneBuildIndex)
    {
        if (CharacterManager.Instance != null) CharacterManager.Instance.PlayClick();
        if (sceneLoader != null)
        {
            sceneLoader.LoadSpecificScene(sceneBuildIndex);
        }
        else if (GameFlowManager.Instance != null)
        {
            Debug.LogWarning("SceneLoader not found. Directly loading scene using SceneManager.LoadScene(int buildIndex).");
            SceneManager.LoadScene(sceneBuildIndex);
        }
        else
        {
            Debug.LogError("No specific scene loader (SceneLoader or GameFlowManager) found. Directly loading scene using SceneManager.LoadScene(int buildIndex).");
            SceneManager.LoadScene(sceneBuildIndex);
        }
    }

    public void OnBackToChapters()
    {
        if (CharacterManager.Instance != null) CharacterManager.Instance.PlayClick();
        
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (chapterSelectionPanel != null) chapterSelectionPanel.SetActive(true);
        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(false);
    }
}