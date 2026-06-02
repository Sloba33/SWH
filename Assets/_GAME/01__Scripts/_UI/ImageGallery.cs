using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.UI;

public class ImageGallery : MonoBehaviour
{
    [Header("Configuration")]
    public int minBuildIndex = 3;
    public int maxBuildIndex = 500;
    public ImageGalleryDataSO imageGalleryDataSO;
    public GameObject gallerySlotPrefab;
    [SerializeField] public LevelProgress[] levelProgressPrefabs;

    [Header("Layout Settings")]
    public int itemsPerRow = 3;
    public float paddingTop = 20f;
    public float paddingLeft = 20f;
    public float spacingX = 15f;
    public float spacingY = 15f;

    [Header("References")]
    public Transform Content;
    public RectTransform viewportRect;
    public ScrollRect scrollRect;
    public TrophyRoadManager trophyRoadManager;
    public GalleryButton galleryButton;
    public Awarder awarder;

    [Header("Pooling")]
    public int bufferRows = 2; // Reduced buffer
    public int minimumPoolSize = 30;
    public bool enableDebug = true;

    private Vector2 cellSize;
    private float cachedCellHeight;

    private readonly List<GameObject> activeSlots = new();
    private readonly Dictionary<int, bool> filledCache = new();
    private readonly Dictionary<int, Image> outlineCache = new();

    public List<int> claimedGalleryRewards = new();

    private int totalItems;
    private int currentStartIndex;

    private bool isPopulated;
    private bool isPopulating;
    private bool isReloading;

    private float lastScrollY = -999f;
    private const float SCROLL_THRESHOLD = 15f; // Increased threshold

    private bool cachedHasUnclaimedRewards;
    private int lastUnclaimedCheckFrame = -1;
    private CanvasGroup canvasGroup;
    public void CloseImageGallery()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public void OpenImageGallery()
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }
    private class SlotData
    {
        public GameObject root;
        public GallerySlotPrefab gallerySlot;
        public LevelProgress levelProgress;
        public int currentDataIndex = -1;
    }
    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (!isPopulated && !isPopulating)
        {
            StartCoroutine(PopulateGalleryCoroutine());
        }
        ScrollToFirstUnclaimedReward();
    }
    private void Update()
    {
        if (enableDebug && Input.GetKeyDown(KeyCode.Y))
        {
            DebugCenterRow();
            DebugVisibleRows();
        }
    }
    private readonly List<SlotData> slotPool = new();

    private void LogDebug(string message)
    {
        if (enableDebug)
            Debug.Log($"[ImageGallery] {message}");
    }

    private void OnEnable()
    {

    }
    public void PopulateOnLoad()
    {
        if (isPopulated || isPopulating)
            return;

        isPopulating = true;
        LogDebug("Starting population...");

        totalItems = levelProgressPrefabs.Length;
        LogDebug($"Total items to display: {totalItems}");

        foreach (Transform child in Content)
        {
            Destroy(child.gameObject);
        }

        activeSlots.Clear();
        slotPool.Clear();
        outlineCache.Clear();

        LoadClaimedGalleryRewards();

        SetCellSizeByViewport();
        SetContentHeight();

        float viewportHeight = viewportRect.rect.height;
        int visibleRows = Mathf.CeilToInt(viewportHeight / cachedCellHeight);
        int totalPoolCount = Mathf.Max(
            minimumPoolSize,
            (visibleRows + (bufferRows * 2)) * itemsPerRow);

        LogDebug($"Viewport height: {viewportHeight}, Visible rows: {visibleRows}, Total pool slots: {totalPoolCount}");

        for (int i = 0; i < totalPoolCount; i++)
        {
            GameObject slotObject = Instantiate(gallerySlotPrefab, Content);

            RectTransform rt = slotObject.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = cellSize;

            LevelProgress lp = Instantiate(levelProgressPrefabs[0], slotObject.transform);
            lp.gameObject.SetActive(false);

            RectTransform progressRect = lp.GetComponent<RectTransform>();

            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = Vector2.one;
            progressRect.pivot = new Vector2(0.5f, 0.5f);
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;
            progressRect.localScale = new Vector3(0.45f, 0.45f, 0.45f);

            SlotData slotData = new SlotData
            {
                root = slotObject,
                gallerySlot = slotObject.GetComponent<GallerySlotPrefab>(),
                levelProgress = lp
            };

            slotPool.Add(slotData);
            activeSlots.Add(slotObject);



        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        currentStartIndex = 0;

        // Force scroll to top
        scrollRect.verticalNormalizedPosition = 1f;

        // Record initial scroll position
        lastScrollY = Content.GetComponent<RectTransform>().anchoredPosition.y;
        LogDebug($"Initial scroll Y position: {lastScrollY}");

        UpdateSlotPositions(currentStartIndex);

        // Load ALL slots initially
        LoadSlotsOnLoad();

        UpdateGalleryButtonGlow();
        isPopulated = true;
        isPopulating = false;
        LogDebug("Population complete!");

        // Debug initial visible rows
        DebugVisibleRows();
    }
    public void LoadSlotsOnLoad()
    {
        LogDebug("Loading all initial slots...");

        for (int i = 0; i < slotPool.Count; i++)
        {
            int dataIndex = currentStartIndex + i;
            if (dataIndex >= totalItems) continue;

            SlotData slot = slotPool[i];

            if (slot.currentDataIndex != dataIndex)
            {
                BindDataToSlot(slot, dataIndex);


            }
        }

        LogDebug("Initial loading complete");
    }
    private IEnumerator PopulateGalleryCoroutine()
    {
        if (isPopulated || isPopulating)
            yield break;

        isPopulating = true;
        LogDebug("Starting population...");

        totalItems = levelProgressPrefabs.Length;
        LogDebug($"Total items to display: {totalItems}");

        foreach (Transform child in Content)
        {
            Destroy(child.gameObject);
        }

        activeSlots.Clear();
        slotPool.Clear();
        outlineCache.Clear();

        LoadClaimedGalleryRewards();

        SetCellSizeByViewport();
        SetContentHeight();

        float viewportHeight = viewportRect.rect.height;
        int visibleRows = Mathf.CeilToInt(viewportHeight / cachedCellHeight);
        int totalPoolCount = Mathf.Max(
            minimumPoolSize,
            (visibleRows + (bufferRows * 2)) * itemsPerRow);

        LogDebug($"Viewport height: {viewportHeight}, Visible rows: {visibleRows}, Total pool slots: {totalPoolCount}");

        for (int i = 0; i < totalPoolCount; i++)
        {
            GameObject slotObject = Instantiate(gallerySlotPrefab, Content);

            RectTransform rt = slotObject.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = cellSize;

            LevelProgress lp = Instantiate(levelProgressPrefabs[0], slotObject.transform);
            lp.gameObject.SetActive(false);

            RectTransform progressRect = lp.GetComponent<RectTransform>();

            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = Vector2.one;
            progressRect.pivot = new Vector2(0.5f, 0.5f);
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;
            progressRect.localScale = new Vector3(0.45f, 0.45f, 0.45f);

            SlotData slotData = new SlotData
            {
                root = slotObject,
                gallerySlot = slotObject.GetComponent<GallerySlotPrefab>(),
                levelProgress = lp
            };

            slotPool.Add(slotData);
            activeSlots.Add(slotObject);

            if (i % 10 == 0)
                yield return null;
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        currentStartIndex = 0;

        // Force scroll to top
        scrollRect.verticalNormalizedPosition = 1f;
        yield return null;
        yield return null; // Two frames to settle

        // Record initial scroll position
        lastScrollY = Content.GetComponent<RectTransform>().anchoredPosition.y;
        LogDebug($"Initial scroll Y position: {lastScrollY}");

        UpdateSlotPositions(currentStartIndex);

        // Load ALL slots initially
        yield return StartCoroutine(LoadAllSlots());

        UpdateGalleryButtonGlow();
        isPopulated = true;
        isPopulating = false;
        LogDebug("Population complete!");

        // Debug initial visible rows
        DebugVisibleRows();
    }

    private IEnumerator LoadAllSlots()
    {
        LogDebug("Loading all initial slots...");

        for (int i = 0; i < slotPool.Count; i++)
        {
            int dataIndex = currentStartIndex + i;
            if (dataIndex >= totalItems) continue;

            SlotData slot = slotPool[i];

            if (slot.currentDataIndex != dataIndex)
            {
                BindDataToSlot(slot, dataIndex);

                if (i % 3 == 0)
                    yield return null;
            }
        }

        LogDebug("Initial loading complete");
    }

    private void SetCellSizeByViewport()
    {
        float width = viewportRect.rect.width;
        float totalSpacing = spacingX * (itemsPerRow - 1);
        float availableWidth = width - (paddingLeft * 2) - totalSpacing;
        float side = availableWidth / itemsPerRow;

        cellSize = new Vector2(side, side);
        cachedCellHeight = cellSize.y + spacingY;

        LogDebug($"Cell size: {cellSize}, Cell height with spacing: {cachedCellHeight}");
    }

    private void SetContentHeight()
    {
        int totalRows = Mathf.CeilToInt(totalItems / (float)itemsPerRow);
        float contentHeight = paddingTop +
            (totalRows * cellSize.y) +
            (Mathf.Max(0, totalRows - 1) * spacingY) +
            spacingY;

        Content.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

        LogDebug($"Total rows: {totalRows}, Content height: {contentHeight}");
    }

    private void DebugVisibleRows()
    {
        float currentScrollY = Content.GetComponent<RectTransform>().anchoredPosition.y;
        float scrollOffset = Mathf.Abs(currentScrollY);

        // Top-based calculation
        float topWithoutPadding = scrollOffset - paddingTop;
        int topRow = Mathf.FloorToInt(topWithoutPadding / cachedCellHeight);
        topRow = Mathf.Max(0, topRow);

        // Center-based calculation
        float viewportCenter = scrollOffset + (viewportRect.rect.height / 2f);
        float centerWithoutPadding = viewportCenter - paddingTop;
        int centerRow = Mathf.FloorToInt(centerWithoutPadding / cachedCellHeight);
        float rowProgress = (centerWithoutPadding % cachedCellHeight) / cachedCellHeight;
        int activeRow = centerRow;
        if (rowProgress > 0.5f) activeRow = centerRow + 1;

        // Bottom-based calculation
        float bottomEdge = scrollOffset + viewportRect.rect.height;
        float bottomWithoutPadding = bottomEdge - paddingTop;
        int bottomRow = Mathf.FloorToInt(bottomWithoutPadding / cachedCellHeight);
        bottomRow = Mathf.Min(Mathf.CeilToInt(totalItems / (float)itemsPerRow) - 1, bottomRow);

        LogDebug($"=== VISIBLE ROWS DEBUG ===");
        LogDebug($"Scroll Y: {currentScrollY}, Scroll offset: {scrollOffset}");
        LogDebug($"TOP visible row: {topRow}");
        LogDebug($"CENTER visible row: {centerRow} (+{rowProgress:F2}) -> ACTIVE row: {activeRow}");
        LogDebug($"BOTTOM visible row: {bottomRow}");
        LogDebug($"Visible rows count: {bottomRow - topRow + 1}");
    }

    private void OnScrollValueChanged(Vector2 pos)
    {
        if (!isPopulated || isReloading) return;

        float currentScrollY = Content.GetComponent<RectTransform>().anchoredPosition.y;
        float scrollDelta = Mathf.Abs(currentScrollY - lastScrollY);

        // Only update if scrolled past threshold to save performance
        if (scrollDelta < SCROLL_THRESHOLD) return;

        lastScrollY = currentScrollY;

        float scrollOffset = Mathf.Abs(currentScrollY);
        float scrollWithoutPadding = scrollOffset - paddingTop;

        // 1. Calculate which row is currently at the top of the screen
        int topVisibleRow = Mathf.FloorToInt(scrollWithoutPadding / cachedCellHeight);
        topVisibleRow = Mathf.Max(0, topVisibleRow);

        // 2. Calculate the start index including the buffer rows above
        int targetStartRow = Mathf.Max(0, topVisibleRow - bufferRows);
        int newStartIndex = targetStartRow * itemsPerRow;

        // 3. Clamp to valid range
        int maxStartIndex = Mathf.Max(0, totalItems - slotPool.Count);
        newStartIndex = Mathf.Min(newStartIndex, maxStartIndex);

        // 4. Only update if the starting row has actually shifted
        if (newStartIndex != currentStartIndex)
        {
            currentStartIndex = newStartIndex;
            // We use a modular refresh to prevent viewport items from flickering
            RefreshActiveSlotsModular();
        }
    }
    private void RefreshActiveSlotsModular()
    {
        int poolSize = slotPool.Count;

        // We iterate through the range of data we want to show
        for (int i = 0; i < poolSize; i++)
        {
            int dataIndex = currentStartIndex + i;

            // Modular math: Find the specific slot in the pool responsible for this data index
            // This ensures the slot doesn't change data if it's still in the visible range
            int slotObjectIndex = dataIndex % poolSize;
            SlotData slot = slotPool[slotObjectIndex];

            if (dataIndex >= totalItems)
            {
                slot.root.SetActive(false);
                slot.currentDataIndex = -1; // Reset so it rebinds if it comes back
                continue;
            }

            slot.root.SetActive(true);

            // 1. Position the slot based on its dataIndex
            int row = dataIndex / itemsPerRow;
            int col = dataIndex % itemsPerRow;
            float x = paddingLeft + (col * (cellSize.x + spacingX));
            float y = -paddingTop - (row * (cellSize.y + spacingY));

            RectTransform rt = slot.root.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);

            // 2. Only Bind if the data index for this specific slot has actually changed
            if (slot.currentDataIndex != dataIndex)
            {
                BindDataToSlot(slot, dataIndex);
            }
        }
    }
    private int lastStableTopRow = -1; // Add this class variable

    private IEnumerator ReloadChangedSlots()
    {
        isReloading = true;

        int reloadCount = 0;

        for (int i = 0; i < slotPool.Count; i++)
        {
            int dataIndex = currentStartIndex + i;
            if (dataIndex >= totalItems) continue;

            SlotData slot = slotPool[i];

            // Only reload if data changed
            if (slot.currentDataIndex != dataIndex)
            {
                reloadCount++;
                if (enableDebug && reloadCount <= 5)
                {
                    int row = dataIndex / itemsPerRow;
                    LogDebug($"Reloading slot: index {slot.currentDataIndex} -> {dataIndex} (Row {row})");
                }
                BindDataToSlot(slot, dataIndex);

                // Yield occasionally
                if (reloadCount % 5 == 0)
                    yield return null;
            }
        }

        if (enableDebug && reloadCount > 0)
            LogDebug($"Reload complete - {reloadCount} slots updated");

        isReloading = false;
    }
    private void UpdateSlotPositions(int startIndex)
    {
        for (int i = 0; i < slotPool.Count; i++)
        {
            int dataIndex = startIndex + i;
            SlotData slot = slotPool[i];

            if (dataIndex >= totalItems)
            {
                slot.root.SetActive(false);
                continue;
            }

            slot.root.SetActive(true);

            int row = dataIndex / itemsPerRow;
            int col = dataIndex % itemsPerRow;

            float x = paddingLeft + (col * (cellSize.x + spacingX));
            float y = -paddingTop - (row * (cellSize.y + spacingY));

            RectTransform rt = slot.root.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
        }
    }

    private void BindDataToSlot(SlotData slot, int dataIndex)
    {
        slot.gallerySlot.ResetSlot();
        slot.currentDataIndex = dataIndex;

        LevelProgress targetPrefab = levelProgressPrefabs[dataIndex];

        // Check if we need to swap the LevelProgress instance
        if (slot.levelProgress.name.Replace("(Clone)", "") != targetPrefab.name)
        {
            Destroy(slot.levelProgress.gameObject);

            slot.levelProgress = Instantiate(targetPrefab, slot.root.transform);

            RectTransform progressRect = slot.levelProgress.GetComponent<RectTransform>();

            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = Vector2.one;
            progressRect.pivot = new Vector2(0.5f, 0.5f);
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;
            progressRect.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        }

        LevelProgress lp = slot.levelProgress;
        lp.gameObject.SetActive(true);

        slot.gallerySlot.currentIndex = dataIndex;
        slot.gallerySlot.levelProgressIndex = dataIndex;
        slot.gallerySlot.imageGallery = this;
        slot.gallerySlot.trophyRoadManager = trophyRoadManager;

        bool claimed = IsGalleryRewardClaimed(dataIndex);
        slot.gallerySlot.isClaimed = claimed;

        lp.GalleryInit();

        bool filled = lp.AreAllImagesFilled();
        filledCache[dataIndex] = filled;

        slot.gallerySlot.isFilled = filled;

        SetupSlotVisuals(slot.gallerySlot, lp, dataIndex, filled);
    }

    private void SetupSlotVisuals(GallerySlotPrefab gallerySlot, LevelProgress lp, int index, bool isFilled)
    {
        gallerySlot.Initialize(index, trophyRoadManager, this);

        if (isFilled && !gallerySlot.isClaimed)
        {
            Image outline = GetOrFindOutlineImage(lp, index);

            if (outline != null)
            {
                outline.gameObject.SetActive(true);

                if (ColorUtility.TryParseHtmlString("#FFB500", out Color gold))
                {
                    outline.color = gold;
                }
            }

            lp.ResetAllImageFills();
            gallerySlot.StartTweening();
        }
        else if (gallerySlot.isClaimed)
        {
            gallerySlot.SetClaimed();

            Image outline = GetOrFindOutlineImage(lp, index);

            if (outline != null)
            {
                outline.gameObject.SetActive(false);
            }

            foreach (Image img in lp.GetComponentsInChildren<Image>(true))
            {
                if (img.type == Image.Type.Filled)
                {
                    img.fillAmount = 1f;
                }
            }
        }
        else
        {
            gallerySlot.SetUnavailable();
        }
    }

    private Image GetOrFindOutlineImage(LevelProgress instance, int index)
    {
        if (outlineCache.TryGetValue(index, out Image cachedImage) && cachedImage != null)
            return cachedImage;

        foreach (Image img in instance.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject.name.IndexOf("outline", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                outlineCache[index] = img;
                return img;
            }
        }

        return null;
    }

    public void ClaimGalleryReward(int levelProgressIndex, GallerySlotPrefab gallerySlot, int xpBubblesToSpawn, int xpGain)
    {
        if (claimedGalleryRewards.Contains(levelProgressIndex))
            return;

        claimedGalleryRewards.Add(levelProgressIndex);
        SaveClaimedGalleryRewards();

        awarder.AwardCurrencyManual(50, gallerySlot.xpPile, gallerySlot.xpPile, gallerySlot.xpPile.GetComponent<RectTransform>());

        gallerySlot.isClaimed = true;

        LevelProgress lp = gallerySlot.GetComponentInChildren<LevelProgress>();

        if (lp != null)
        {
            SetupSlotVisuals(gallerySlot, lp, levelProgressIndex, true);
        }

        lastUnclaimedCheckFrame = -1;
        UpdateGalleryButtonGlow();
    }
    [NaughtyAttributes.Button("Test")]
    public void Test()
    {
        Debug.Log("Test");
    }
    private bool IsGalleryRewardClaimed(int index)
    {
        return claimedGalleryRewards.Contains(index);
    }

    private void SaveClaimedGalleryRewards()
    {
        PlayerPrefs.SetString("ClaimedGalleryRewards", string.Join(",", claimedGalleryRewards));
        PlayerPrefs.Save();
    }

    private void LoadClaimedGalleryRewards()
    {
        string data = PlayerPrefs.GetString("ClaimedGalleryRewards", string.Empty);

        if (!string.IsNullOrEmpty(data))
        {
            claimedGalleryRewards = new List<int>(System.Array.ConvertAll(data.Split(','), int.Parse));
        }
    }

    private bool HasUnclaimedRewards()
    {
        if (Time.frameCount == lastUnclaimedCheckFrame)
            return cachedHasUnclaimedRewards;

        lastUnclaimedCheckFrame = Time.frameCount;

        for (int i = 0; i < totalItems; i++)
        {
            if (filledCache.TryGetValue(i, out bool filled) && filled && !IsGalleryRewardClaimed(i))
            {
                cachedHasUnclaimedRewards = true;
                return true;
            }
        }

        cachedHasUnclaimedRewards = false;
        return false;
    }

    private void UpdateGalleryButtonGlow()
    {
        if (galleryButton != null)
        {
            galleryButton.SetClaimable(HasUnclaimedRewards());
        }
    }

    public void ScrollToFirstUnclaimedReward()
    {
        if (!isPopulated)
        {
            StartCoroutine(ScrollAfterPopulation());
            return;
        }

        int targetIndex = -1;

        for (int i = 0; i < totalItems; i++)
        {
            if (filledCache.TryGetValue(i, out bool filled) && filled && !IsGalleryRewardClaimed(i))
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex == -1)
            return;

        LogDebug($"Scrolling to first unclaimed reward at index: {targetIndex}");

        int targetRow = targetIndex / itemsPerRow;
        float targetY = paddingTop + (targetRow * cachedCellHeight);
        float maxScroll = Content.GetComponent<RectTransform>().rect.height - viewportRect.rect.height;

        scrollRect.verticalNormalizedPosition = maxScroll > 0 ? Mathf.Clamp01(1f - (targetY / maxScroll)) : 1f;

        StartCoroutine(UpdateAfterScroll(targetRow));
    }

    private IEnumerator UpdateAfterScroll(int targetRow)
    {
        yield return new WaitForSeconds(0.15f);

        int newStartRow = Mathf.Max(0, targetRow - bufferRows);
        int newStartIndex = newStartRow * itemsPerRow;
        newStartIndex = Mathf.Min(newStartIndex, Mathf.Max(0, totalItems - slotPool.Count));

        if (newStartIndex != currentStartIndex)
        {
            LogDebug($"After scroll - Updating to start index: {newStartIndex}");
            currentStartIndex = newStartIndex;
            UpdateSlotPositions(currentStartIndex);

            if (!isReloading)
                StartCoroutine(ReloadChangedSlots());
        }
    }
    private void DebugCenterRow()
    {
        float currentScrollY = Content.GetComponent<RectTransform>().anchoredPosition.y;
        float scrollOffset = Mathf.Abs(currentScrollY);
        float viewportCenter = scrollOffset + (viewportRect.rect.height / 2f);
        float centerWithoutPadding = viewportCenter - paddingTop;
        int centerRow = Mathf.FloorToInt(centerWithoutPadding / cachedCellHeight);
        float rowProgress = (centerWithoutPadding % cachedCellHeight) / cachedCellHeight;
        int activeRow = centerRow;
        if (rowProgress > 0.5f) activeRow = centerRow + 1;

        LogDebug($"=== CENTER ROW DEBUG ===");
        LogDebug($"Scroll Y: {currentScrollY}");
        LogDebug($"Viewport center: {viewportCenter}");
        LogDebug($"Center row: {centerRow}, Progress: {rowProgress:F2}, Active row: {activeRow}");
        LogDebug($"Current window rows: {currentStartIndex / itemsPerRow} to {(currentStartIndex + slotPool.Count) / itemsPerRow}");
    }
    private IEnumerator ScrollAfterPopulation()
    {
        while (isPopulating)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();
        ScrollToFirstUnclaimedReward();
    }
}