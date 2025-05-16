using System.Collections;
using System.Collections.Generic;
using DanielLochner.Assets.SimpleScrollSnap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePassManager : MonoBehaviour
{
    public bool hasPremium;
    public Transform battlePassPanel;
    public BattlePassData battlePassData;
    public List<int> claimedFreeItems = new List<int>();
    public List<int> claimedPremiumItems = new List<int>();
    public Transform battlePassFillParent;
    public List<BattlePassFill> battlePassFills = new List<BattlePassFill>();
    public Image BPFillBarGlobal;
    public TextMeshProUGUI BPFillBarText;
    public TextMeshProUGUI BPLevelTextGlobal;

    private void Start()
    {
        customScrollBattlePass = FindFirstObjectByType<CustomScrollBattlePass>(FindObjectsInactive.Include);
        SetCurrencyAtStart();
        for (int i = 0; i < battlePassFillParent.childCount; i++)
        {
            battlePassFills.Add(battlePassFillParent.GetChild(i).GetComponent<BattlePassFill>());
            int lvltxt = (i + 2);
            battlePassFills[i].levelText.text = lvltxt + "";
        }

        GenerateButtons();
        UpdateFillBars();
        LoadClaimedItems();
        CheckForUnlockedItems();
        UpdateButtons();
        UpdateUnclaimedCounter();
        // Check for unclaimed free items and set claimable status
        bool hasUnclaimedFreeItems = CheckUnclaimedFreeItems();
        SetClaimable(hasUnclaimedFreeItems);
    }

    private Sprite GetSpriteForRewardType(RewardType rewardType)
    {
        return rewardType switch
        {
            RewardType.Coins_Small => battlePassData.coinSprite_Small,
            RewardType.Gems_Small => battlePassData.gemSprite_Small,
            RewardType.Money_Small => battlePassData.moneySprite_Small,
            RewardType.Coins_Medium => battlePassData.coinSprite_Medium,
            RewardType.Gems_Medium => battlePassData.gemSprite_Medium,
            RewardType.Money_Medium => battlePassData.moneySprite_Medium,
            RewardType.Coins_Large => battlePassData.coinSprite_Large,
            RewardType.Gems_Large => battlePassData.gemSprite_Large,
            RewardType.Money_Large => battlePassData.moneySprite_Large,
            _ => null,
        };
    }
    private Sprite GetBackgroundForRewardType(RewardType rewardType)
    {
        return rewardType switch
        {
            RewardType.Coins_Small or RewardType.Coins_Medium or RewardType.Coins_Large => battlePassData.backgroundYellow,
            RewardType.Gems_Small or RewardType.Gems_Medium or RewardType.Gems_Large => battlePassData.backgroundPink,
            RewardType.Money_Small or RewardType.Money_Medium or RewardType.Money_Large => battlePassData.backgroundGreen,
            _ => null,
        };
    }
    private AudioClip GetAudioClipForRewardType(RewardType rewardType)
    {
        return rewardType switch
        {
            RewardType.Coins_Small or RewardType.Coins_Medium or RewardType.Coins_Large => battlePassData.audioClipGold,
            RewardType.Gems_Small or RewardType.Gems_Medium or RewardType.Gems_Large => battlePassData.audioClipGems,
            RewardType.Money_Small or RewardType.Money_Medium or RewardType.Money_Large => battlePassData.audioClipGold,
            _ => null,
        };
    }
    public void CheckForUnlockedItems()
    {
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        foreach (var slot in battlePassData.slots)
        {
            Debug.Log("Slot level : " + slot.level + " Player level " + playerLevel);
            if (slot.level <= playerLevel)
            {
                if (slot.freeItem != null && !IsItemClaimed(slot.level, true))
                {
                    // Free item is unlocked but not yet claimed
                    Debug.Log(
                        $"Unlocked free item: {slot.freeItem.description} at level {slot.level}"
                    );
                    // Notify player or update UI
                }
                if (slot.premiumItem != null && !IsItemClaimed(slot.level, false))
                {
                    // Premium item is unlocked but not yet claimed
                    Debug.Log(
                        $"Unlocked premium item: {slot.premiumItem.description} at level {slot.level}"
                    );
                    // Notify player or update UI
                }
            }
        }
    }

    public void ClaimItem(int level, bool isFree, BattlePassButton bpbutton)
    {
        if (!IsItemClaimed(level, isFree))
        {
            if (isFree)
            {
                claimedFreeItems.Add(level);
                GrantReward(battlePassData.slots[level - 1].freeItem); // Assuming levels are 1-indexed
            }
            else
            {
                claimedPremiumItems.Add(level);
                GrantReward(battlePassData.slots[level - 1].premiumItem); // Assuming levels are 1-indexed
            }
            SaveClaimedItems();
            Debug.Log($"Claimed item at level {level} (Free: {isFree})");
            // Update UI or give the item to the player
            UpdateButtons();
            // UpdateButton(bpbutton, isFree);
            UpdateUnclaimedCounter();
            bool hasUnclaimedFreeItems = CheckUnclaimedFreeItems();
            SetClaimable(hasUnclaimedFreeItems);
        }
        StartCoroutine(ScrollToNextItem());
    }

    private void GrantReward(BattlePassItem item)
    {
        switch (item.rewardType)
        {
            case RewardType.Coins_Small or RewardType.Coins_Medium or RewardType.Coins_Large:
                // Grant coins
                Debug.Log($"Granted {item.amount} coins.");
                AddCurrency("coins", item.amount);
                break;

            case RewardType.Gems_Small or RewardType.Gems_Medium or RewardType.Gems_Large:
                // Grant gems
                Debug.Log($"Granted {item.amount} gems.");
                AddCurrency("gems", item.amount);
                break;

            case RewardType.Money_Small or RewardType.Money_Medium or RewardType.Money_Large:
                // Grant money
                Debug.Log($"Granted {item.amount} money.");
                AddCurrency("money", item.amount);
                break;

            default:
                Debug.LogWarning("Unknown reward type.");
                break;
        }
    }

    public TextMeshProUGUI coinText,
        moneyText,
        gemsText;

    public void AddCurrency(string currency, int amount)
    {
        PlayerPrefs.SetInt(currency, PlayerPrefs.GetInt(currency) + amount);
        switch (currency)
        {
            case "coins":
                coinText.text = PlayerPrefs.GetInt(currency) + "";
                break;
            case "diamonds":
                gemsText.text = PlayerPrefs.GetInt(currency) + "";
                break;
            case "money":
                moneyText.text = PlayerPrefs.GetInt(currency) + "";
                break;
        }
    }

    public void SetCurrencyAtStart()
    {
        coinText.text = PlayerPrefs.GetInt("coins") + "";
        gemsText.text = PlayerPrefs.GetInt("gems") + "";
        moneyText.text = PlayerPrefs.GetInt("money") + "";
    }

    private bool IsItemClaimed(int level, bool isFree)
    {
        return isFree ? claimedFreeItems.Contains(level) : claimedPremiumItems.Contains(level);
    }

    private void SaveClaimedItems()
    {
        PlayerPrefs.SetString("ClaimedFreeItems", string.Join(",", claimedFreeItems));
        PlayerPrefs.SetString("ClaimedPremiumItems", string.Join(",", claimedPremiumItems));
        PlayerPrefs.Save();
    }

    private void LoadClaimedItems()
    {
        string claimedFreeItemsString = PlayerPrefs.GetString("ClaimedFreeItems", string.Empty);
        string claimedPremiumItemsString = PlayerPrefs.GetString(
            "ClaimedPremiumItems",
            string.Empty
        );

        if (!string.IsNullOrEmpty(claimedFreeItemsString))
        {
            claimedFreeItems = new List<int>(
                System.Array.ConvertAll(claimedFreeItemsString.Split(','), int.Parse)
            );
        }
        if (!string.IsNullOrEmpty(claimedPremiumItemsString))
        {
            claimedPremiumItems = new List<int>(
                System.Array.ConvertAll(claimedPremiumItemsString.Split(','), int.Parse)
            );
        }
    }

    public void UpdateFillBars()
    {
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        int currentLevelProgress = PlayerPrefs.GetInt("XP_IN_LEVEL", 0);
        int requiredXP = 100 + (((playerLevel - 1) * 20)); // Adjust this formula based on your leveling system
        Debug.Log("Current level : " + playerLevel + " Current progress within the level : " + currentLevelProgress + " required XP to reach next level : " + requiredXP);


        for (int i = 0; i < battlePassFills.Count; i++)
        {
            if (i + 1 < playerLevel) // Levels are 1-indexed
            {
                battlePassFills[i].fillBar.fillAmount = 1f;
                battlePassFills[i].levelFrame.color = battlePassFills[i].reached;
            }
            else if (i + 1 == playerLevel)
            {
                battlePassFills[i].fillBar.fillAmount = (float)currentLevelProgress / requiredXP;
                battlePassFills[i].levelFrame.color = battlePassFills[i].notReached;
            }
            else
            {
                battlePassFills[i].fillBar.fillAmount = 0f;
                battlePassFills[i].levelFrame.color = battlePassFills[i].notReached;
            }
        }
    }

    public Transform freeButtonContainer,
        premiumButtonContainer;
    public GameObject battlePassButton; // Prefab for the claim button

    public void UpdateButtons()
    {
        BattlePassButton[] freeButtons =
            freeButtonContainer.GetComponentsInChildren<BattlePassButton>();
        BattlePassButton[] premiumButtons =
            premiumButtonContainer.GetComponentsInChildren<BattlePassButton>();
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        foreach (var button in freeButtons)
        {
            bool isClaimed = IsItemClaimed(button.Level, button.IsFree);
            bool isClaimable = button.Level <= playerLevel;
            button.SetButtonState(isClaimable, isClaimed);
        }

        foreach (var button in premiumButtons)
        {
            if (hasPremium)
            {

                bool isClaimed = IsItemClaimed(button.Level, button.IsFree);
                bool isClaimable = button.Level <= playerLevel;
                button.SetButtonState(isClaimable, isClaimed);
            }
            else button.SetButtonNoPremium();
        }
    }
    public void UpdateButton(BattlePassButton button, bool isFree)
    {
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        bool isClaimed = IsItemClaimed(button.Level, button.IsFree);
        bool isClaimable = button.Level <= playerLevel;
        button.SetButtonState(isClaimable, isClaimed);

    }

    public GameObject freeButtonPrefab,
        premiumButtonPrefab;

    private void GenerateButtons()
    {
        foreach (var slot in battlePassData.slots)
        {
            Sprite freeItemSprite = GetSpriteForRewardType(slot.freeItem.rewardType);
            Sprite premiumItemSprite = GetSpriteForRewardType(slot.premiumItem.rewardType);
            Sprite backgroundSpriteFree = GetBackgroundForRewardType(slot.freeItem.rewardType);
            Sprite backgroundSpritePremium = GetBackgroundForRewardType(slot.premiumItem.rewardType);
            // freeItemSprite.GetComponent<Image>().SetNativeSize();
            // premiumItemSprite.GetComponent<Image>().SetNativeSize();
            // Assuming you have a reference to the Image component where the sprite is being set





            // Fit the images within the 125x125 box


            // Free item button
            if (slot.freeItem != null)
            {
                GameObject freeButtonObj = Instantiate(freeButtonPrefab, freeButtonContainer);
                BattlePassButton freeButton = freeButtonObj.GetComponent<BattlePassButton>();
                freeButton.Initialize(
                    slot.level,
                    slot.freeItem.amount.ToString(),
                    freeItemSprite,
                    "CLAIM",
                    true,
                    this,
                    backgroundSpriteFree,
                    battlePassPanel,
                    GetAudioClipForRewardType(slot.freeItem.rewardType)
                );
                FitImageToBox(freeButton.itemImage, 125, 125);
            }

            // Premium item button
            if (slot.premiumItem != null)
            {
                GameObject premiumButtonObj = Instantiate(
                    premiumButtonPrefab,
                    premiumButtonContainer
                );
                BattlePassButton premiumButton = premiumButtonObj.GetComponent<BattlePassButton>();
                premiumButton.Initialize(
                    slot.level,
                    slot.premiumItem.amount.ToString(),
                    premiumItemSprite,
                    "CLAIM",
                    false,
                    this,
                    backgroundSpritePremium,
                    battlePassPanel,
                    GetAudioClipForRewardType(slot.premiumItem.rewardType)
                );
                FitImageToBox(premiumButton.itemImage, 125, 125);
            }
        }

        UpdateButtons();
    }
    void FitImageToBox(Image image, float maxWidth, float maxHeight)
    {
        if (image.sprite == null) return;

        // Get the sprite's actual size
        float spriteWidth = image.sprite.rect.width;
        float spriteHeight = image.sprite.rect.height;

        // Calculate the scale factor to fit within the max dimensions
        float scaleFactor = Mathf.Min(maxWidth / spriteWidth, maxHeight / spriteHeight);

        // Set the RectTransform's si1zeDelta to the scaled size
        image.rectTransform.sizeDelta = new Vector2(spriteWidth * scaleFactor, spriteHeight * scaleFactor);
    }
    public Sprite claimableImage,
        unclaimableImage;

    public void SetClaimable(bool claimable)
    {
        if (claimable)
            battlePassButton.GetComponent<Image>().sprite = claimableImage;
        else
            battlePassButton.GetComponent<Image>().sprite = unclaimableImage;
    }
    private CustomScrollBattlePass customScrollBattlePass;
    public IEnumerator ScrollToNextItem()
    {
        yield return new WaitForSeconds(1f); // Lowered the wait time. Or change to yield return null; to remove wait.
        Debug.Log("scrolling to item" + " " + unclaimedIndex);
        customScrollBattlePass.CenterOnSelectedItem(unclaimedIndex);
    }
    public int unclaimedIndex = 0;
    private bool CheckUnclaimedFreeItems()
    {
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        unclaimedIndex = 0; // Reset unclaimedIndex

        // First, check for any unclaimed items within the player's level
        for (int i = 0; i < battlePassData.slots.Length; i++)
        {
            var slot = battlePassData.slots[i];
            if (slot.level <= playerLevel && slot.freeItem != null && !IsItemClaimed(slot.level, true))
            {
                unclaimedIndex = i;
                return true; // Found an unclaimed item within the player's level
            }
        }

        // If no unclaimed items within the player's level, find the next item in line
        for (int i = 0; i < battlePassData.slots.Length; i++)
        {
            var slot = battlePassData.slots[i];
            if (slot.level > playerLevel && slot.freeItem != null)
            {
                unclaimedIndex = i;
                return false; // Found the next item in line (unclaimable)
            }
        }

        // If no next item found (all items claimed or no items left), return false
        return false;
    }
    public TextMeshProUGUI unclaimedCounterText;
    public GameObject unclaimedObjectImage;
    public void UpdateUnclaimedCounter()
    {
        int unclaimedCount = 0;
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        foreach (var slot in battlePassData.slots)
        {
            if (
                slot.level <= playerLevel
                && slot.freeItem != null
                && !IsItemClaimed(slot.level, true)
            )
            {
                unclaimedCount++;
            }
        }
        unclaimedObjectImage.SetActive(true);
        unclaimedCounterText.text = unclaimedCount.ToString();
        if (unclaimedCount == 0)
        {
            unclaimedObjectImage.SetActive(false);
        }
    }
}
