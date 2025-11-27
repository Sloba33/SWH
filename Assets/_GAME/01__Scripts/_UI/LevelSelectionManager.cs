using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Coffee.UIEffects;

public class LevelSelectionManager : MonoBehaviour
{
    private bool suppressScrollSelection = false;
    private float suppressScrollTimer = 0f;
    [SerializeField] private float scrollSuppressDuration = 0.3f; // duration to block scroll updates after click
    private bool initialized = false;
    private const string LastSelectedChapterKey = "LastSelectedChapterIndex";

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
    public RectTransform levelContentTransform;    // CONTENT (GridLayoutGroup on this)
    public GridLayoutGroup levelGrid;              // Assign the GridLayoutGroup component on levelContentTransform
    public RectTransform levelViewport;            // Assign the viewport RectTransform used by levels ScrollRect
    public RectTransform levelButtonPrefab;
    public GameObject checkmarkPrefab;
    public List<Level> allLevels;

    [Header("Preload / Performance")]
    [Tooltip("When true, all chapters' level buttons are instantiated once on start and reused (recommended).")]
    public bool preloadAllChapters = true;

    [Header("Level Appearance Animation")]
    public bool animateLevelsSequentially;
    public bool animateLevelsScalePop;
    public bool animateLevelsFadeSlide;

    [Header("Level Scrolling")]
    [Tooltip("If true, scrolling to a target row is animated.")]
    public bool smoothLevelScroll = true;
    [Tooltip("Higher = faster scroll animation.")]
    public float smoothLevelScrollSpeed = 8f;

    [SerializeField] private RectTransform chapterSnapMarker;

    private SceneLoader sceneLoader;

    // Internal pooled list for preloaded buttons (if using preload)
    // Each child is the instantiated button (parented to levelContentTransform).
    private List<RectTransform> preloadedButtons = new List<RectTransform>();

    private void Awake()
    {
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        sceneLoader = FindObjectOfType<SceneLoader>();

        if (backToChaptersButton != null)
            backToChaptersButton.onClick.AddListener(OnBackToChapters);

        // Display chapters (build chapter buttons)
        InitializeLevelSelectionUI();

        if (chapterSnapMarker != null && chapterScrollRect != null)
        {
            chapterSnapMarker.SetParent(chapterScrollRect.viewport, false);
            chapterSnapMarker.anchorMin = new Vector2(0, 1);
            chapterSnapMarker.anchorMax = new Vector2(1, 1);
            chapterSnapMarker.pivot = new Vector2(0.5f, 1f);
            chapterSnapMarker.anchoredPosition = new Vector2(0, topPixelOffset);
            chapterSnapMarker.sizeDelta = new Vector2(0, 4);
        }

        ScrollDragListener dragListener = chapterScrollRect != null ? chapterScrollRect.GetComponent<ScrollDragListener>() : null;
        if (dragListener != null)
            dragListener.onEndDrag.AddListener(OnChapterScrollEndDrag);

        if (useChapters)
            StartCoroutine(AutoSelectStoredChapterNextFrame());
    }

    private IEnumerator AutoSelectStoredChapterNextFrame()
    {
        yield return null; // wait for layout
        if (allChapterDefinitions.Count == 0 || chapterContentTransform.childCount == 0)
            yield break;

        int storedIndex = PlayerPrefs.GetInt(LastSelectedChapterKey, 0);
        storedIndex = Mathf.Clamp(storedIndex, 0, allChapterDefinitions.Count - 1);

        ChapterDefinition def = allChapterDefinitions[storedIndex];
        RectTransform rt = chapterContentTransform.GetChild(storedIndex) as RectTransform;

        currentSelectedChapter = rt;
        currentSelectedChapterDef = def;

        // Show correct chapter visuals + levels
        DisplayLevelsForChapter(def);
        SetSelectedChapterVisuals(rt);

        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(true);
        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.05f);

        if (rt != null)
        {
            if (snapCoroutine != null)
                StopCoroutine(snapCoroutine);
            snapCoroutine = StartCoroutine(SnapToChapterSmooth(rt));
        }

        initialized = true;
    }

    private void Update()
    {
        if (suppressScrollSelection)
        {
            suppressScrollTimer -= Time.deltaTime;
            if (suppressScrollTimer <= 0f)
                suppressScrollSelection = false;
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
    public GameObject levelSelectionPanelRoot; 
    public void OpenLevelSelection()
    {
        levelSelectionPanelRoot.SetActive(true);
        // If not using preload, clear as before. If preload, keep preloaded children.
        if (!preloadAllChapters)
        {
            ClearContentPanel(chapterContentTransform);
            ClearContentPanel(levelContentTransform);
        }
        else
        {
            ClearContentPanel(chapterContentTransform);
            // Do NOT Clear levelContentTransform — preloaded buttons will be reused
        }

        if (chapterSelectionPanel != null) chapterSelectionPanel.SetActive(false);
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(false);

        InitializeLevelSelectionUI();

        if (useChapters)
            StartCoroutine(RestoreSelectionNextFrame());
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
            // If not using chapters and preload is enabled, preload single "chapter" (all levels)
            if (preloadAllChapters)
                PreloadAllLevels();
            DisplayAllLevels();
        }

        // If using preload, ensure levels are created now (once)
        if (preloadAllChapters && useChapters)
        {
            PreloadAllLevels();
        }
    }

    private IEnumerator RestoreSelectionNextFrame()
    {
        // Wait for the UI to rebuild and for children to exist
        yield return null;
        yield return new WaitForEndOfFrame();

        if (currentSelectedChapterDef != null)
        {
            int index = allChapterDefinitions.IndexOf(currentSelectedChapterDef);
            if (index >= 0 && index < chapterContentTransform.childCount)
            {
                currentSelectedChapter = chapterContentTransform.GetChild(index) as RectTransform;
            }
            else
            {
                int storedIndex = PlayerPrefs.GetInt(LastSelectedChapterKey, 0);
                storedIndex = Mathf.Clamp(storedIndex, 0, Mathf.Max(0, chapterContentTransform.childCount - 1));
                if (storedIndex < chapterContentTransform.childCount)
                    currentSelectedChapter = chapterContentTransform.GetChild(storedIndex) as RectTransform;
            }
        }
        else
        {
            int storedIndex = PlayerPrefs.GetInt(LastSelectedChapterKey, 0);
            storedIndex = Mathf.Clamp(storedIndex, 0, Mathf.Max(0, chapterContentTransform.childCount - 1));
            if (storedIndex < chapterContentTransform.childCount)
            {
                currentSelectedChapter = chapterContentTransform.GetChild(storedIndex) as RectTransform;
                currentSelectedChapterDef = allChapterDefinitions.Count > storedIndex ? allChapterDefinitions[storedIndex] : null;
            }
        }

        if (currentSelectedChapter != null && currentSelectedChapterDef != null)
        {
            DisplayLevelsForChapter(currentSelectedChapterDef);

            SetSelectedChapterVisuals(currentSelectedChapter);

            if (levelSelectionPanel != null) levelSelectionPanel.SetActive(true);
            if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(true);

            if (snapCoroutine != null) StopCoroutine(snapCoroutine);
            snapCoroutine = StartCoroutine(SnapToChapterSmooth(currentSelectedChapter));
        }
        else
        {
            if (chapterContentTransform.childCount > 0)
            {
                RectTransform first = chapterContentTransform.GetChild(0) as RectTransform;
                currentSelectedChapter = first;
                currentSelectedChapterDef = allChapterDefinitions.Count > 0 ? allChapterDefinitions[0] : null;
                SetSelectedChapterVisuals(first);
            }
        }
        initialized = true;
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

        // If we cleared the levelContentTransform (legacy mode) also clear preloaded list
        if (contentParent == levelContentTransform)
        {
            preloadedButtons.Clear();
        }
    }

    public float topPixelOffset = 160f;
    private void UpdateChapterSelectionByScroll()
    {
        if (chapterContentTransform == null || chapterContentTransform.childCount == 0) return;

        float minDistance = float.MaxValue;
        RectTransform closestChapter = null;
        ChapterDefinition selectedDef = null;

        Vector3 worldTargetPosition = chapterScrollRect.viewport.TransformPoint(new Vector3(0, topPixelOffset, 0));

        for (int i = 0; i < chapterContentTransform.childCount; i++)
        {
            RectTransform chapterRT = chapterContentTransform.GetChild(i) as RectTransform;
            Vector3 worldChapterPos = chapterRT.position;

            float distance = Mathf.Abs(worldChapterPos.y - worldTargetPosition.y);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestChapter = chapterRT;
                selectedDef = allChapterDefinitions[i];
            }
        }

        if (closestChapter != null && closestChapter != currentSelectedChapter)
        {
            SetSelectedChapterVisuals(closestChapter);
            DisplayLevelsForChapter(selectedDef);

            currentSelectedChapter = closestChapter;
            currentSelectedChapterDef = selectedDef;

            int chapterIndex = allChapterDefinitions.IndexOf(selectedDef);
            if (chapterIndex >= 0)
            {
                PlayerPrefs.SetInt(LastSelectedChapterKey, chapterIndex);
                PlayerPrefs.SetString("LastSelectedChapter", selectedDef.chapterName);
                PlayerPrefs.Save();
            }
        }

        if (selectedDef != null)
            Debug.Log($"[Scroll] Selected Chapter: {selectedDef.chapterName}");
    }

    private Coroutine snapCoroutine;

    public void OnScrollValueChanged(Vector2 pos)
    {
        if (!initialized) return;
        if (suppressScrollSelection) return; // Ignore scroll updates right after a click
        UpdateChapterSelectionByScroll();
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

            Vector3 targetScale = isSelected ? Vector3.one * 1.2f : Vector3.one;
            StartCoroutine(AnimateScale(child, targetScale, duration));

            Chapter chapter = child.GetComponent<Chapter>();
            if (chapter != null)
            {
                if (chapter.chapterImage != null)
                    StartCoroutine(FadeCanvasImageAlpha(chapter.chapterImage, isSelected ? selectedAlpha : targetAlpha, duration));

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
            if (target != null)
                target.localScale = Vector3.Lerp(from, toScale, t / duration);
            yield return null;
        }

        if (target != null) target.localScale = toScale;
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
            if (chapterMono != null)
            {
                chapterMono.chapterImage.sprite = chapterDef.chapterSprite;
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

    private IEnumerator AnimateLevelsSequentially()
    {
        float delay = 0.001f;
        float scaleDuration = 0.02f;

        for (int i = 0; i < levelContentTransform.childCount; i++)
        {
            RectTransform level = levelContentTransform.GetChild(i) as RectTransform;
            level.localScale = Vector3.zero;
            level.gameObject.SetActive(true);

            float t = 0f;
            while (t < scaleDuration)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, t / scaleDuration);
                if (level != null) level.localScale = Vector3.one * s;
                yield return null;
            }

            if (level != null) level.localScale = Vector3.one;
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator AnimateLevelsScalePop()
    {
        float scaleDuration = 0.15f;
        for (int i = 0; i < levelContentTransform.childCount; i++)
        {
            RectTransform level = levelContentTransform.GetChild(i) as RectTransform;
            level.localScale = Vector3.zero;
        }

        yield return null;

        float t = 0f;
        while (t < scaleDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, t / scaleDuration);
            for (int i = 0; i < levelContentTransform.childCount; i++)
            {
                RectTransform level = levelContentTransform.GetChild(i) as RectTransform;
                if (level != null)
                    level.localScale = Vector3.one * s;
            }
            yield return null;
        }

        for (int i = 0; i < levelContentTransform.childCount; i++)
        {
            RectTransform level = levelContentTransform.GetChild(i) as RectTransform;
            level.localScale = Vector3.one;
        }
    }

    private IEnumerator AnimateLevelsFadeSlide()
    {
        float delayBetween = 0.03f;
        float animDuration = 0.1f;

        for (int i = 0; i < levelContentTransform.childCount; i++)
        {
            RectTransform level = levelContentTransform.GetChild(i) as RectTransform;
            CanvasGroup cg = level.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = level.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            Vector3 startPos = level.anchoredPosition + new Vector2(0, -50f);
            Vector3 endPos = level.anchoredPosition;
            level.anchoredPosition = startPos;

            StartCoroutine(FadeSlideLevel(level, cg, startPos, endPos, animDuration));
            yield return new WaitForSeconds(delayBetween);
        }
    }

    private IEnumerator FadeSlideLevel(RectTransform level, CanvasGroup cg, Vector3 startPos, Vector3 endPos, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            if (cg != null) cg.alpha = p;
            if (level != null) level.anchoredPosition = Vector3.Lerp(startPos, endPos, p);
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;
        if (level != null) level.anchoredPosition = endPos;
    }

    public void OnChapterSelected(ChapterDefinition selectedChapterDef)
    {
        if (CharacterManager.Instance != null) CharacterManager.Instance.PlayClick();

        if (currentSelectedChapterDef == selectedChapterDef)
            return;

        int index = allChapterDefinitions.IndexOf(selectedChapterDef);
        if (index >= 0 && index < chapterContentTransform.childCount)
        {
            RectTransform clickedRT = chapterContentTransform.GetChild(index) as RectTransform;
            currentSelectedChapter = clickedRT;
            currentSelectedChapterDef = selectedChapterDef;

            suppressScrollSelection = true;
            suppressScrollTimer = scrollSuppressDuration;

            PlayerPrefs.SetInt(LastSelectedChapterKey, index);
            PlayerPrefs.SetString("LastSelectedChapter", selectedChapterDef.chapterName);
            PlayerPrefs.Save();

            SetSelectedChapterVisuals(clickedRT);
            DisplayLevelsForChapter(selectedChapterDef);

            if (snapCoroutine != null)
                StopCoroutine(snapCoroutine);
            snapCoroutine = StartCoroutine(SnapToChapterSmooth(clickedRT));
        }

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

        // If not preloading, keep original behaviour: clear+instantiate
        if (!preloadAllChapters)
        {
            ClearContentPanel(levelContentTransform);

            if (chapterDef.levelsInChapter == null || chapterDef.levelsInChapter.Count == 0)
            {
                Debug.LogWarning($"Chapter '{chapterDef.chapterName}' has no Level assets assigned. No levels to display.");
                return;
            }

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

            // Resize content dynamically now that children exist
            int chapterIndex = allChapterDefinitions.IndexOf(chapterDef);
            ResizeLevelContentForChapter(chapterIndex);

            // scroll so next available level row is at top
            int nextLevel = FindNextAvailableLevelInChapter(chapterDef);
            ScrollLevelRowToTop(nextLevel, smoothLevelScroll);

            if (animateLevelsSequentially)
                StartCoroutine(AnimateLevelsSequentially());
            else if (animateLevelsScalePop)
                StartCoroutine(AnimateLevelsScalePop());
            else if (animateLevelsFadeSlide)
                StartCoroutine(AnimateLevelsFadeSlide());

            return;
        }

        // --- PRELOAD MODE: show/hide preloaded children based on metadata ---
        if (chapterDef.levelsInChapter == null || chapterDef.levelsInChapter.Count == 0)
        {
            Debug.LogWarning($"Chapter '{chapterDef.chapterName}' has no Level assets assigned. No levels to display.");
            return;
        }

        chapterDef.levelsInChapter.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

        int chapterIdx = allChapterDefinitions.IndexOf(chapterDef);
        int currentUnlocked = PlayerPrefs.GetInt("Level", 0);

        // Activate only buttons that belong to this chapter; also update their visuals (interactable/checkmarks/ui effects)
        int shownCount = 0;
        for (int i = 0; i < preloadedButtons.Count; i++)
        {
            RectTransform btn = preloadedButtons[i];
            LevelMeta meta = btn.GetComponent<LevelMeta>();
            if (meta == null)
            {
                btn.gameObject.SetActive(false);
                continue;
            }

            if (meta.chapterIndex == chapterIdx)
            {
                btn.gameObject.SetActive(true);
                shownCount++;

                // Update visuals based on level data (in case PlayerPrefs changed since preload)
                Level level = meta.associatedLevel;
                Button btnComp = btn.GetComponent<Button>();
                LevelButtonDisplay levelDisplay = btn.GetComponent<LevelButtonDisplay>();

                if (btnComp != null && level != null)
                {
                    btnComp.interactable = level.levelNumber <= (currentUnlocked + 1);

                    bool isLatest = (level.levelNumber == (currentUnlocked + 1));
                    UIShiny uiShiny = btn.GetComponent<UIShiny>();
                    UIEffectTweener uiEffectTweener = btn.GetComponent<UIEffectTweener>();
                    if (uiShiny != null) uiShiny.enabled = isLatest;
                    if (uiEffectTweener != null) uiEffectTweener.enabled = isLatest;
                }

                // Ensure checkmark shows for completed levels (avoid duplicates)
                if (checkmarkPrefab != null && level != null && level.levelNumber <= currentUnlocked)
                {
                    bool hasCheck = btn.Find("Checkmark(Clone)") != null;
                    if (!hasCheck)
                    {
                        GameObject checkmarkInstance = Instantiate(checkmarkPrefab, btn);
                        RectTransform checkmarkRect = checkmarkInstance.GetComponent<RectTransform>();
                        if (checkmarkRect != null)
                        {
                            checkmarkRect.anchorMin = new Vector2(1, 1);
                            checkmarkRect.anchorMax = new Vector2(1, 1);
                            checkmarkRect.pivot = new Vector2(1, 1);
                            checkmarkRect.anchoredPosition = new Vector2(-10, -10);
                        }
                    }
                }
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        // Force layout rebuild then resize content based on number of levels in chapter
        LayoutRebuilder.ForceRebuildLayoutImmediate(levelContentTransform);
        int nextAvailable = FindNextAvailableLevelInChapter(chapterDef);
        ResizeLevelContentForChapter(chapterIdx);

        // Scroll to next available level (so its row sits at top)
        ScrollLevelRowToTop(nextAvailable, smoothLevelScroll);

        // Play animations (affects active children)
        if (animateLevelsSequentially)
            StartCoroutine(AnimateLevelsSequentially());
        else if (animateLevelsScalePop)
            StartCoroutine(AnimateLevelsScalePop());
        else if (animateLevelsFadeSlide)
            StartCoroutine(AnimateLevelsFadeSlide());
    }

    private void DisplayAllLevels()
    {
        if (levelContentTransform == null)
        {
            Debug.LogError("LevelSelectionManager: Level Content Transform is not assigned! Cannot display all levels.");
            return;
        }

        if (!preloadAllChapters)
        {
            ClearContentPanel(levelContentTransform);
            if (backToChaptersButton != null) backToChaptersButton.gameObject.SetActive(false);

            if (allLevels == null || allLevels.Count == 0)
            {
                Debug.LogWarning("LevelSelectionManager: No Level assets assigned to 'allLevels' list. Please assign them in the Inspector if 'useChapters' is false.");
                return;
            }

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

            // Resize to fit all levels (single "chapter")
            ResizeLevelContentForCount(allLevels.Count);
        }
        else
        {
            // Preload mode with single list: ensure preloaded exist and show those whose chapterIndex == -1 (or all)
            if (preloadedButtons.Count == 0)
                PreloadAllLevels(); // fallback

            foreach (RectTransform btn in preloadedButtons)
            {
                btn.gameObject.SetActive(true);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(levelContentTransform);
            ResizeLevelContentForCount(preloadedButtons.Count);
        }
    }

    private void InstantiateLevelButton(Level level, int currentUnlockedLevel, Transform contentParent)
    {
        if (levelButtonPrefab == null)
        {
            Debug.LogError("LevelSelectionManager: Level Button Prefab is not assigned!");
            return;
        }

        if (level.sceneBuildIndex < 0 || level.sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"Level '{level.levelNumber}' (Scene Build Index: {level.sceneBuildIndex}) has an invalid sceneBuildIndex. Scene not found in Build Settings. Button will not be created.");
            return;
        }

        RectTransform levelButtonGO = Instantiate(levelButtonPrefab, contentParent);
        Button buttonComponent = levelButtonGO.GetComponent<Button>();
        LevelButtonDisplay levelButtonDisplay = levelButtonGO.GetComponent<LevelButtonDisplay>();
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
            buttonComponent.interactable = level.levelNumber <= (currentUnlockedLevel + 1);

            bool isLatestUnbeatenLevel = (level.levelNumber == (currentUnlockedLevel + 1));
            if (levelButtonDisplay != null)
            {
                levelButtonDisplay.levelSceneName = level.sceneName;
                levelButtonDisplay.Initialize();
            }
            else
            {
                Debug.LogWarning($"Level button prefab '{levelButtonPrefab.name}' is missing a LevelButtonDisplay component.");
            }

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

            if (checkmarkPrefab != null && level.levelNumber <= currentUnlockedLevel)
            {
                GameObject checkmarkInstance = Instantiate(checkmarkPrefab, levelButtonGO.transform);
                RectTransform checkmarkRect = checkmarkInstance.GetComponent<RectTransform>();

                if (checkmarkRect != null)
                {
                    checkmarkRect.anchorMin = new Vector2(1, 1);
                    checkmarkRect.anchorMax = new Vector2(1, 1);
                    checkmarkRect.pivot = new Vector2(1, 1);
                    checkmarkRect.anchoredPosition = new Vector2(-10, -10);
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

    // -------------------- NEW: PRELOAD LOGIC --------------------

    private void PreloadAllLevels()
    {
        // If already preloaded, no-op but refresh meta
        if (preloadedButtons.Count > 0)
        {
            // ensure meta.associatedLevel is set
            return;
        }

        if (levelButtonPrefab == null)
        {
            Debug.LogError("LevelSelectionManager: Level Button Prefab is not assigned! Cannot preload levels.");
            return;
        }

        // Create all buttons for all chapters and keep them (inactive except for shown chapter)
        for (int c = 0; c < allChapterDefinitions.Count; c++)
        {
            ChapterDefinition def = allChapterDefinitions[c];
            if (def == null || def.levelsInChapter == null) continue;

            def.levelsInChapter.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            for (int li = 0; li < def.levelsInChapter.Count; li++)
            {
                Level level = def.levelsInChapter[li];

                RectTransform btn = Instantiate(levelButtonPrefab, levelContentTransform);
                btn.gameObject.name = $"CH{c}_Level_{level.levelNumber}";
                btn.gameObject.SetActive(false); // initially hidden; DisplayLevelsForChapter will show relevant
                LevelMeta meta = btn.gameObject.AddComponent<LevelMeta>();
                meta.chapterIndex = c;
                meta.levelNumber = level.levelNumber;
                meta.associatedLevel = level;

                preloadedButtons.Add(btn);

                // Initialize visuals now
                Button btnComp = btn.GetComponent<Button>();
                LevelButtonDisplay levelButtonDisplay = btn.GetComponent<LevelButtonDisplay>();
                TextMeshProUGUI tmpText = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = level.levelNumber.ToString();
                if (levelButtonDisplay != null)
                {
                    levelButtonDisplay.levelSceneName = level.sceneName;
                    levelButtonDisplay.Initialize();
                }

                int currentUnlocked = PlayerPrefs.GetInt("Level", 0);
                if (btnComp != null)
                {
                    btnComp.interactable = level.levelNumber <= (currentUnlocked + 1);
                    int sceneToLoadIndex = level.sceneBuildIndex;
                    btnComp.onClick.AddListener(() => OnLevelSelected(sceneToLoadIndex));
                }

                // Add checkmark if necessary
                if (checkmarkPrefab != null && level.levelNumber <= PlayerPrefs.GetInt("Level", 0))
                {
                    GameObject checkmarkInstance = Instantiate(checkmarkPrefab, btn);
                    RectTransform checkmarkRect = checkmarkInstance.GetComponent<RectTransform>();
                    if (checkmarkRect != null)
                    {
                        checkmarkRect.anchorMin = new Vector2(1, 1);
                        checkmarkRect.anchorMax = new Vector2(1, 1);
                        checkmarkRect.pivot = new Vector2(1, 1);
                        checkmarkRect.anchoredPosition = new Vector2(-10, -10);
                    }
                }
            }
        }

        // Force a rebuild so Content size is correct when first shown
        LayoutRebuilder.ForceRebuildLayoutImmediate(levelContentTransform);
    }

    // -------------------- NEW: RESIZE/SCROLL HELPERS --------------------

    private void ResizeLevelContentForChapter(int chapterIndex)
    {
        if (levelGrid == null || levelContentTransform == null) return;
        int count = 0;
        if (preloadAllChapters)
        {
            // count how many preloaded buttons have this chapterIndex
            for (int i = 0; i < preloadedButtons.Count; i++)
            {
                LevelMeta m = preloadedButtons[i].GetComponent<LevelMeta>();
                if (m != null && m.chapterIndex == chapterIndex) count++;
            }
        }
        else
        {
            // if not preloaded, count children in content (they are only this chapter)
            count = levelContentTransform.childCount;
        }

        ResizeLevelContentForCount(count);
    }

    private void ResizeLevelContentForCount(int count)
    {
        if (levelGrid == null || levelContentTransform == null || levelViewport == null) return;

        int columns = Mathf.Max(1, levelGrid.constraintCount);
        int rows = Mathf.CeilToInt(count / (float)columns);
        float cellH = levelGrid.cellSize.y;
        float spacingY = levelGrid.spacing.y;
        float padTop = levelGrid.padding.top;
        float padBottom = levelGrid.padding.bottom;

        float height = padTop + padBottom + rows * cellH + Mathf.Max(0, rows - 1) * spacingY;

        levelContentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        LayoutRebuilder.ForceRebuildLayoutImmediate(levelContentTransform);
    }

    private int FindNextAvailableLevelInChapter(ChapterDefinition def)
    {
        if (def == null || def.levelsInChapter == null || def.levelsInChapter.Count == 0) return 0;
        def.levelsInChapter.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
        int unlocked = PlayerPrefs.GetInt("Level", 0);

        // Next available is the first level whose levelNumber == unlocked + 1 (or first if none)
        for (int i = 0; i < def.levelsInChapter.Count; i++)
        {
            if (def.levelsInChapter[i].levelNumber == unlocked + 1)
                return i;
        }

        // fallback - return 0 (first)
        return 0;
    }

    /// <summary>
    /// Scrolls the levels ScrollRect so that the row containing the given levelIndex (index within chapter) is positioned at the top of the viewport.
    /// levelIndex is index within the chapter (0-based). If invalid, it will clamp.
    /// </summary>
    private void ScrollLevelRowToTop(int levelIndexInChapter, bool useSmooth)
    {
        if (levelGrid == null || levelContentTransform == null || levelViewport == null) return;
        // clamp
        int chapterIdx = allChapterDefinitions.IndexOf(currentSelectedChapterDef);
        int count = 0;
        if (chapterIdx >= 0 && chapterIdx < allChapterDefinitions.Count)
            count = allChapterDefinitions[chapterIdx].levelsInChapter.Count;

        if (count == 0) return;

        levelIndexInChapter = Mathf.Clamp(levelIndexInChapter, 0, count - 1);

        int columns = Mathf.Max(1, levelGrid.constraintCount);
        int row = levelIndexInChapter / columns;

        float cellH = levelGrid.cellSize.y;
        float spacingY = levelGrid.spacing.y;
        float padTop = levelGrid.padding.top;

        // Top edge of the row measured from top of content (y increasing downward in anchoredPosition)
        // We need distance from top: rowTop = padTop + row * (cellH + spacingY)
        float rowTop = padTop + row * (cellH + spacingY);

        // Convert that position to a normalized verticalNormalizedPosition:
        float contentHeight = levelContentTransform.rect.height;
        float viewportHeight = levelViewport.rect.height;
        float maxScroll = contentHeight - viewportHeight;
        if (maxScroll <= 0)
        {
            // no scroll needed
            return;
        }

        // requiredScroll is pixels from top to put that row at top
        float requiredScroll = rowTop;
        float normalizedY = Mathf.Clamp01(1f - (requiredScroll / maxScroll));

        if (useSmooth)
        {
            StartCoroutine(SmoothScrollLevelTo(normalizedY));
        }
        else
        {
            // find ScrollRect controlling levels (if any)
            ScrollRect sr = levelContentTransform.GetComponentInParent<ScrollRect>();
            if (sr != null)
                sr.verticalNormalizedPosition = normalizedY;
        }
    }

    private IEnumerator SmoothScrollLevelTo(float targetNormalized)
    {
        ScrollRect sr = levelContentTransform.GetComponentInParent<ScrollRect>();
        if (sr == null) yield break;
        float start = sr.verticalNormalizedPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * smoothLevelScrollSpeed;
            sr.verticalNormalizedPosition = Mathf.Lerp(start, targetNormalized, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        sr.verticalNormalizedPosition = targetNormalized;
    }
}

// ------------------- small helper/meta component for preloaded buttons -------------------
public class LevelMeta : MonoBehaviour
{
    public int chapterIndex = -1;
    public int levelNumber = -1; // human level number
    public Level associatedLevel;
}
