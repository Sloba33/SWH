// LevelSelectionManager.cs (MODIFIED: UIShiny and UIEffectTweener control)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Coffee.UIEffects;
using System.Collections;
using Fusion;

public class LevelSelectionManager : MonoBehaviour
{
    [Header("Chapters")]
    public bool useChapters = false;
    public GameObject chapterSelectionPanel;
    public RectTransform chapterContentTransform;
    public Button backToChaptersButton;
    public RectTransform chapterButtonPrefab;
    public List<ChapterDefinition> allChapterDefinitions;
    public ScrollRect chapterScrollRect;
    private RectTransform currentSelectedChapter = null;
    private ChapterDefinition currentSelectedChapterDef = null;
    [SerializeField] private float snapDuration = 0.25f;
    [Header("Levels")]

    public GameObject levelSelectionPanel;
    public RectTransform levelContentTransform;


    public RectTransform levelButtonPrefab;
    public GameObject checkmarkPrefab;

    public List<Level> allLevels;

    private SceneLoader sceneLoader;
    [SerializeField] private RectTransform chapterSnapMarker;
    private void Awake()
    {
        // if (chapterSelectionPanel != null) chapterSelectionPanel.SetActive(false);
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
        if (chapterSnapMarker != null && chapterScrollRect != null)
        {
            chapterSnapMarker.SetParent(chapterScrollRect.viewport, false);
            chapterSnapMarker.anchorMin = new Vector2(0, 1);
            chapterSnapMarker.anchorMax = new Vector2(1, 1);
            chapterSnapMarker.pivot = new Vector2(0.5f, 1f);
            chapterSnapMarker.anchoredPosition = new Vector2(0, topPixelOffset);
            chapterSnapMarker.sizeDelta = new Vector2(0, 4); // Full width, 4px height
        }
        ScrollDragListener dragListener = chapterScrollRect.GetComponent<ScrollDragListener>();
        if (dragListener != null)
        {
            dragListener.onEndDrag.AddListener(OnChapterScrollEndDrag);
        }
        if (useChapters)
        {
            StartCoroutine(AutoSelectFirstChapterNextFrame());
        }

    }
    private IEnumerator AutoSelectFirstChapterNextFrame()
    {
        yield return null; // Wait for layout

        if (allChapterDefinitions.Count == 0 || chapterContentTransform.childCount == 0)
            yield break;

        ChapterDefinition firstDef = allChapterDefinitions[0];
        RectTransform firstRT = chapterContentTransform.GetChild(0) as RectTransform;

        // Call selection logic (displays levels, updates visuals)
        OnChapterSelected(firstDef);

        // Extra safety wait to allow layout & scroll to stabilize
        yield return new WaitForSeconds(0.05f);

        if (firstRT != null)
        {
            SetSelectedChapterVisuals(firstRT);
            currentSelectedChapter = firstRT;
            currentSelectedChapterDef = firstDef;

            if (snapCoroutine != null)
                StopCoroutine(snapCoroutine);
            snapCoroutine = StartCoroutine(SnapToChapterSmooth(firstRT));
        }
    }
    private void OnChapterScrollEndDrag()
    {
        if (snapCoroutine != null)
            StopCoroutine(snapCoroutine);

        snapCoroutine = StartCoroutine(SnapToChapterSmooth(currentSelectedChapter));
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
    public float topPixelOffset = 160f;
    private void UpdateChapterSelectionByScroll()
    {
        float minDistance = float.MaxValue;
        RectTransform closestChapter = null;
        ChapterDefinition selectedDef = null;

        Vector3 worldTargetPosition = chapterScrollRect.viewport.TransformPoint(new Vector3(0, topPixelOffset, 0)); // ~top area

        for (int i = 0; i < chapterContentTransform.childCount; i++)
        {
            RectTransform chapterRT = chapterContentTransform.GetChild(i) as RectTransform;
            Vector3 worldChapterPos = chapterRT.position;

            float distance = Mathf.Abs(worldChapterPos.y - worldTargetPosition.y);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestChapter = chapterRT;
                selectedDef = allChapterDefinitions[i]; // assuming same order
            }
        }

        if (closestChapter != null && closestChapter != currentSelectedChapter)
        {
            SetSelectedChapterVisuals(closestChapter);
            DisplayLevelsForChapter(selectedDef);

            currentSelectedChapter = closestChapter;
            currentSelectedChapterDef = selectedDef;
        }
        Debug.Log($"Selected: {selectedDef.chapterName}");
    }
    private Coroutine snapCoroutine;

    public void OnScrollValueChanged(Vector2 pos)
    {
        UpdateChapterSelectionByScroll();

        // if (snapCoroutine != null)
        //     StopCoroutine(snapCoroutine);
        // snapCoroutine = StartCoroutine(WaitThenSnap());

    }

    private IEnumerator WaitThenSnap()
    {
        yield return new WaitForSeconds(0.1f);

        // Wait until scroll slows down (velocity is low)
        while (chapterScrollRect.velocity.magnitude > 50f)
        {
            yield return null;
        }

        yield return SnapToChapterSmooth(currentSelectedChapter);
    }

    private IEnumerator SnapToChapterSmooth(RectTransform chapterRT)
    {
        if (chapterRT == null) yield break;

        Vector3 worldTarget = chapterScrollRect.viewport.TransformPoint(new Vector3(0, topPixelOffset, 0));
        Vector3 localTarget = chapterScrollRect.content.InverseTransformPoint(worldTarget);
        Vector3 localChapter = chapterScrollRect.content.InverseTransformPoint(chapterRT.position);

        float deltaY = localTarget.y - localChapter.y;
        Vector2 startPos = chapterScrollRect.content.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, deltaY);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / snapDuration;
            chapterScrollRect.content.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        chapterScrollRect.content.anchoredPosition = endPos;
    }
    private void SetSelectedChapterVisuals(RectTransform selected)
    {
        float duration = 0.2f; // duration for scale/opacity animations
        float targetAlpha = 0.5f;
        float selectedAlpha = 1f;

        for (int i = 0; i < chapterContentTransform.childCount; i++)
        {
            RectTransform child = chapterContentTransform.GetChild(i) as RectTransform;
            bool isSelected = (child == selected);

            // Smooth scale (optional: use DOTween or LeanTween for smoother easing)
            Vector3 targetScale = isSelected ? Vector3.one * 1.2f : Vector3.one;
            // StopAllCoroutines(); // Optional: Prevent overlapping coroutines
            StartCoroutine(AnimateScale(child, targetScale, duration));

            Chapter chapter = child.GetComponent<Chapter>();
            if (chapter != null)
            {
                // Image fade
                if (chapter.chapterImage != null)
                    StartCoroutine(FadeCanvasImageAlpha(chapter.chapterImage, isSelected ? selectedAlpha : targetAlpha, duration));

                // Text fade
                if (chapter.chapterNameText != null)
                    StartCoroutine(FadeTextAlpha(chapter.chapterNameText, isSelected ? selectedAlpha : targetAlpha, duration));
            }
        }
    }
    private IEnumerator AnimateScale(Transform target, Vector3 toScale, float duration)
    {
        Vector3 from = target.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(from, toScale, t / duration);
            yield return null;
        }

        target.localScale = toScale;
    }
    private IEnumerator FadeCanvasImageAlpha(Image img, float toAlpha, float duration)
    {
        Color fromColor = img.color;
        float startAlpha = fromColor.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, toAlpha, t / duration);
            img.color = new Color(fromColor.r, fromColor.g, fromColor.b, alpha);
            yield return null;
        }

        img.color = new Color(fromColor.r, fromColor.g, fromColor.b, toAlpha);
    }
    private IEnumerator FadeTextAlpha(TextMeshProUGUI text, float toAlpha, float duration)
    {
        Color fromColor = text.color;
        float startAlpha = fromColor.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, toAlpha, t / duration);
            text.color = new Color(fromColor.r, fromColor.g, fromColor.b, alpha);
            yield return null;
        }

        text.color = new Color(fromColor.r, fromColor.g, fromColor.b, toAlpha);
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

        // Find the RectTransform of the clicked chapter
        int index = allChapterDefinitions.IndexOf(selectedChapterDef);
        if (index >= 0 && index < chapterContentTransform.childCount)
        {
            RectTransform clickedChapterRT = chapterContentTransform.GetChild(index) as RectTransform;

            SetSelectedChapterVisuals(clickedChapterRT);
            DisplayLevelsForChapter(selectedChapterDef);

            currentSelectedChapter = clickedChapterRT;
            currentSelectedChapterDef = selectedChapterDef;

            if (snapCoroutine != null)
                StopCoroutine(snapCoroutine);
            snapCoroutine = StartCoroutine(SnapToChapterSmooth(clickedChapterRT));
        }
        else
        {
            Debug.LogWarning("Clicked chapter definition not found in list.");
        }

        // if (chapterSelectionPanel != null) chapterSelectionPanel.SetActive(false);
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(true);
        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(true);
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