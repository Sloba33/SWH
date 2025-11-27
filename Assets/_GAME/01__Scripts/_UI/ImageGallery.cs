using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageGallery : MonoBehaviour
{
    [SerializeField] public int minBuildIndex = 3;
    [SerializeField] public int maxBuildIndex = 500;
    public ImageGalleryDataSO imageGalleryDataSO;
    public GameObject gallerySlotPrefab;
    [SerializeField] public LevelProgress[] levelProgressPrefabs;
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
                Debug.Log("Images are filled and reward is unclaimed."); // Corrected log message
                gallerySlot.Initialize(i, trophyRoadManager, this);

                // --- Start of new code to find and color the outline image ---
                Image outlineImage = null;

                // 1. Try finding it as the first child of the LevelProgress instance
                if (levelProgressInstance.transform.childCount > 0)
                {
                    Transform firstChild = levelProgressInstance.transform.GetChild(0);
                    if (firstChild != null && firstChild.name.ToLower().Contains("outline"))
                    {
                        Debug.Log("Found outline image as first child: " + firstChild.name);
                        outlineImage = firstChild.GetComponent<Image>();
                    }
                }

                // 2. If not found as the first child, or if the name doesn't match, search all children recursively
                if (outlineImage == null)
                {
                    // Search recursively in children (including inactive ones)
                    foreach (Image img in levelProgressInstance.GetComponentsInChildren<Image>(true))
                    {
                        if (img.gameObject.name.ToLower().Contains("outline"))
                        {
                            outlineImage = img;
                            break; // Found it, stop searching
                        }
                    }
                }

                if (outlineImage != null)
                {
                    // Change color to #FFB500 (Unity ColorUtility can parse hex strings)
                    Color outlineColor;
                    if (ColorUtility.TryParseHtmlString("#FFB500", out outlineColor))
                    {
                        outlineImage.color = outlineColor;
                        Debug.Log($"Set outline color for {outlineImage.name} in slot {i} to #FFB500.");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to parse color #FFB500 for outline image in slot {i}.");
                    }

                }
                else
                {
                    Debug.LogWarning($"Outline Image not found for slot {i}. Ensure it exists and its name contains 'outline'.");
                }
                // --- End of new code ---

                // --- Call the new method here to set image fills to 0 ---
                levelProgressInstance.ResetAllImageFills();
                Debug.Log($"Reset fills to 0 for LevelProgress instance in slot {i} because it's unclaimed but claimable.");
                // --- End of new code ---

                // gallerySlot.claimButton.onClick.AddListener(() => ClaimGalleryReward(i, gallerySlot));
            }

            else if (IsGalleryRewardClaimed(i))
            {
                Debug.Log("Images are filled and reward has been claimed.");
                gallerySlot.Initialize(i, trophyRoadManager, this);
                gallerySlot.SetClaimed();

                // --- Disable outline for claimed items ---
                Image outlineImage = null;

                // 1. Try first child
                if (levelProgressInstance.transform.childCount > 0)
                {
                    Transform firstChild = levelProgressInstance.transform.GetChild(0);
                    Debug.Log("Looking at object: " + firstChild.name);
                    if (firstChild != null && firstChild.name.ToLower().Contains("outline"))
                    {
                        outlineImage = firstChild.GetComponent<Image>();
                        Debug.Log("Assigned targetted  " + outlineImage.name);
                    }
                }

                // 2. Fallback to recursive search
                if (outlineImage == null)
                {
                    Debug.Log("Targeting img : " + levelProgressInstance.GetComponentsInChildren<Image>());
                    foreach (Image img in levelProgressInstance.GetComponentsInChildren<Image>(true))
                    {
                        if (img.gameObject.name.ToLower().Contains("outline"))
                        {
                            outlineImage = img;
                            Debug.Log("Targetted image was null : " + outlineImage.name);
                            break;
                        }
                    }
                }

                if (outlineImage != null)
                {
                    outlineImage.gameObject.SetActive(false);
                    Debug.Log($"[Init] Disabled outline for claimed reward in slot {i}");
                }
            }
            else
            {
                Debug.Log("Images are not filled.");
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


        Transform slotTransform = Content.GetChild(levelProgressIndex);
        LevelProgress levelProgress = slotTransform.GetComponentInChildren<LevelProgress>();
        levelProgress.Initialize();
        if (levelProgress != null)
        {
            // Set all images to full fill
            foreach (Image img in levelProgress.GetComponentsInChildren<Image>(true))
            {
                if (img != null && img.type == Image.Type.Filled)
                {
                    img.fillAmount = 1f;
                    Debug.Log($"Set {img.name} fill to 1f after claiming reward {levelProgressIndex}");
                }
            }

            // Disable outline image
            Image outlineImage = null;

            foreach (Image img in levelProgress.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name.ToLower().Contains("outline"))
                {
                    outlineImage = img;
                    break;
                }
            }

            if (outlineImage != null)
            {
                outlineImage.gameObject.SetActive(false);
                Debug.Log($"Disabled outline image {outlineImage.name} for claimed reward {levelProgressIndex}");
            }
        }
        else
        {
            Debug.LogWarning($"LevelProgress not found at index {levelProgressIndex}");
        }
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
