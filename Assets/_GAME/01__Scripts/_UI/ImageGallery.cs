using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageGallery : MonoBehaviour
{
    public ImageGalleryDataSO imageGalleryDataSO;
    public GameObject gallerySlotPrefab;
    public LevelProgress[] levelProgressPrefabs;
    public Transform Content;
    private List<int> claimedGalleryRewards = new List<int>();
    public bool AreAllImagesFilled;
    public TrophyRoadManager trophyRoadManager;
    public TrophyRoadData trophyroadData;
    public GridLayoutGroup gridLayout;
    public RectTransform viewportRect;
    private bool initialized;
    public ScrollRect scrollRect;
    public ClaimPanel claimPanelPrefab; // Assign the ClaimPanel prefab in the Inspector
    public int levelProgressIndex;

    public void Initialize()
    {
        trophyRoadManager = FindObjectOfType<TrophyRoadManager>();
        LoadClaimedGalleryRewards();
        PopulateGallery();
        ResizeGridCells();
        initialized = true;
        Debug.Log("Initialized");
        ScrollToFirstUnclaimedReward();

    }
    void Start()
    {
        if (!initialized)
        {

            Initialize();
        }
    }


    private void ShowClaimPanel(int rewardIndex)
    {
        Debug.Log("Reward Index : " + rewardIndex);
        if (imageGalleryDataSO == null || rewardIndex >= imageGalleryDataSO.rewards.Count || claimPanelPrefab == null)
        {
            Debug.LogError("Image gallery null" + imageGalleryDataSO);
            Debug.LogError("rewardIndex >= imageGalleryDataSO.rewards.Count " + (rewardIndex >= imageGalleryDataSO.rewards.Count));
            Debug.LogError("claimPanelPrefab == null" + claimPanelPrefab == null);
            return;
        }

        ImageGalleryDataSO.RewardData reward = imageGalleryDataSO.rewards[rewardIndex];
        ClaimPanel claimPanel = Instantiate(claimPanelPrefab, transform);
        claimPanel.SetRewardData(reward);
        ScrollToFirstUnclaimedReward();
    }
    public void PopulateGallery()
    {
        int i = 0;
        foreach (LevelProgress levelProgressPrefab in levelProgressPrefabs)
        {
            // Instantiate the gallery slot
            GameObject gallerySlotObject = Instantiate(gallerySlotPrefab, Content);

            // Instantiate the LevelProgress prefab within the slot
            LevelProgress levelProgressInstance = Instantiate(levelProgressPrefab, gallerySlotObject.transform);

            levelProgressInstance.transform.localScale = Vector3.one;
            // Fit the LevelProgress prefab within the slot
            RectTransform slotRect = gallerySlotObject.GetComponent<RectTransform>();
            RectTransform progressRect = levelProgressInstance.GetComponent<RectTransform>();

            // Ensure the progress instance keeps the same scale.
            progressRect.localScale = new Vector3(0.45f, 0.45f, 0.45f);

            // Set the progress instance to fill the slot.
            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = Vector2.one;
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;

            // Set the fill amounts from PlayerPrefs
            levelProgressInstance.GalleryInit(); // Initialize to load data.
            AreAllImagesFilled = levelProgressInstance.AreAllImagesFilled();
            GallerySlotPrefab gallerySlot = gallerySlotObject.GetComponent<GallerySlotPrefab>();
            Debug.Log("Are images filled :" + levelProgressInstance.AreAllImagesFilled());
            gallerySlot.trophyRoadManager = trophyRoadManager;
            gallerySlot.imageGallery = this;
            if (AreAllImagesFilled && !IsGalleryRewardClaimed(i))
            {
                Debug.Log("Images are filled");
                gallerySlot.Initialize(i, trophyRoadManager, this);
                // gallerySlot.claimButton.onClick.AddListener(() => ClaimGalleryReward(i, gallerySlot));
            }
            else if (IsGalleryRewardClaimed(i))
            {
                Debug.Log("Images are filled but unclaimed");
                gallerySlot.Initialize(i, trophyRoadManager, this);
                gallerySlot.SetClaimed();
            }
            else
            {
                Debug.Log("Images is not filled");
                gallerySlot.Initialize(i, trophyRoadManager, this);
                gallerySlot.SetUnavailable();
            }

            i++;
        }
    }
    public void ClaimGalleryReward(int levelProgressIndex, GallerySlotPrefab gallerySlot)
    {
        claimedGalleryRewards.Add(levelProgressIndex);
        SaveClaimedGalleryRewards();

        Debug.Log("Spawning panel in ImageGallery");
        ShowClaimPanel(levelProgressIndex);
    }

    private bool IsGalleryRewardClaimed(int levelProgressIndex)
    {
        return claimedGalleryRewards.Contains(levelProgressIndex);
    }

    private void SaveClaimedGalleryRewards()
    {
        PlayerPrefs.SetString("ClaimedGalleryRewards", string.Join(",", claimedGalleryRewards));
        PlayerPrefs.Save();
    }

    private void LoadClaimedGalleryRewards()
    {
        string claimedRewardsString = PlayerPrefs.GetString("ClaimedGalleryRewards", string.Empty);
        if (!string.IsNullOrEmpty(claimedRewardsString))
        {
            claimedGalleryRewards = new List<int>(System.Array.ConvertAll(claimedRewardsString.Split(','), int.Parse));
        }
    }
    void ResizeGridCells()
    {
        if (gridLayout == null || viewportRect == null)
        {
            Debug.LogError("GridLayoutGroup or Viewport RectTransform not assigned!");
            return;
        }

        float viewportWidth = viewportRect.rect.width;
        float spacing = gridLayout.spacing.x; // Horizontal spacing
        float paddingLeft = gridLayout.padding.left;
        float paddingRight = gridLayout.padding.right;

        // Calculate the available width for each cell
        float availableWidth = viewportWidth - paddingLeft - paddingRight - (spacing * 2); // 2 spaces for 3 columns

        // Calculate the cell width
        float cellWidth = availableWidth / 3f;

        // Apply to GridLayoutGroup
        gridLayout.cellSize = new Vector2(cellWidth, cellWidth); // Assuming square images, adjust height as needed.
    }
    public void ScrollToFirstUnclaimedReward()
    {
        if (scrollRect == null || Content == null || Content.childCount == 0)
        {
            Debug.LogWarning("ScrollRect, Content, or children are missing.");
            return;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(Content.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
        int firstUnclaimedIndex = -1;
        int lastClaimedIndex = -1;

        // Iterate through instantiated gallery slots
        for (int i = 0; i < Content.childCount; i++)
        {
            Transform slot = Content.GetChild(i);
            LevelProgress levelProgress = slot.GetComponentInChildren<LevelProgress>();
            if (levelProgress == null) continue;

            bool isClaimed = IsGalleryRewardClaimed(i);
            bool isFilled = levelProgress.AreAllImagesFilled();

            if (isFilled && !isClaimed)
            {
                if (firstUnclaimedIndex == -1)
                {
                    firstUnclaimedIndex = i;
                    Debug.Log($"First unclaimed at index {i}");
                }
            }
            else if (isClaimed)
            {
                lastClaimedIndex = i;
                Debug.Log($"Last claimed at index {i}");
            }
        }

        int targetIndex = (firstUnclaimedIndex != -1) ? firstUnclaimedIndex : lastClaimedIndex;
        if (targetIndex == -1)
        {
            Debug.Log("No target to scroll to.");
            return;
        }

        // Calculate scroll position based on GridLayout
        GridLayoutGroup grid = Content.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            Debug.LogError("GridLayoutGroup missing on Content.");
            return;
        }

        int columns = grid.constraintCount;
        int row = targetIndex / columns;

        float cellHeight = grid.cellSize.y;
        float spacingY = grid.spacing.y;
        float paddingTop = grid.padding.top;

        // Position of the target row's top edge
        float rowTop = paddingTop + row * (cellHeight + spacingY);

        RectTransform contentRect = Content.GetComponent<RectTransform>();
        float contentHeight = contentRect.rect.height;
        float viewportHeight = viewportRect.rect.height;

        // Position to center the row in the viewport
        float requiredScroll = rowTop + (cellHeight / 2f) - (viewportHeight / 2f);
        float maxScroll = contentHeight - viewportHeight;

        if (maxScroll <= 0)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            return;
        }

        float normalizedY = Mathf.Clamp01(1f - (requiredScroll / maxScroll));
        Canvas.ForceUpdateCanvases(); // Ensure layout is updated
        scrollRect.verticalNormalizedPosition = normalizedY;

        Debug.Log($"Scrolling to index {targetIndex} (row {row}), normalized Y: {normalizedY}");

    }
}
