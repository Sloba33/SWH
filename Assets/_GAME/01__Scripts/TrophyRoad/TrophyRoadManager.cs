using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrophyRoadManager : MonoBehaviour
{
    public GameObject trophyTutorialHint, trophyTutorialHintBack;
    public Transform trophyRoadPanel;
    public TrophyRoadData trophyRoadData;
    public List<int> claimedRewards = new List<int>();
    public Transform trophyRoadFillParent;
    public List<TrophyRoadFill> trophyRoadFills = new List<TrophyRoadFill>();
    public Image TRFillBarGlobal;
    public GameObject pointer;
    public TextMeshProUGUI pointerText;
    public GameObject exclamationMark;
    public CharacterTokenManager characterTokenManager;
    public CustomScrollTrophyRoad customScrollTrophyRoad;
    public Transform mainMenuUI;
    public Transform tokenPanelSpawnParent;

    private void Start()
    {
        // PlayerPrefs.SetInt("Trophies", 23);
        customScrollTrophyRoad = FindObjectOfType<CustomScrollTrophyRoad>(true);
        characterTokenManager = FindObjectOfType<CharacterTokenManager>(true);
        SetTrophiesAtStart();
            Debug.Log("Trophy road child count :" + trophyRoadFillParent.childCount);
            Debug.Log("Trophy road child count :" + trophyRoadData.milestones.Count);
        // for (int i = 0; i < trophyRoadFillParent.childCount; i++)
        // {
        //     if (trophyRoadFillParent.GetChild(i).GetComponent<TrophyRoadFill>() != null)
        //     {
        //         Debug.Log("Looking at child :" +i +" and comparing with :"+ trophyRoadData.milestones[i].trophyRequirement);
        //         trophyRoadFills.Add(trophyRoadFillParent.GetChild(i).GetComponent<TrophyRoadFill>());
        //         int trophyCount = trophyRoadData.milestones[i].trophyRequirement;
        //         trophyRoadFills[i].trophyText.text = trophyRoadData.milestones[i].trophyRequirement.ToString();
        //     }
        // }
        for (int i = 0; i < trophyRoadData.milestones.Count; i++)
        {
            if (trophyRoadFillParent.GetChild(i).GetComponent<TrophyRoadFill>() != null)
            {
                Debug.Log("Looking at child :" +i +" and comparing with :"+ trophyRoadData.milestones[i].trophyRequirement);
                trophyRoadFills.Add(trophyRoadFillParent.GetChild(i).GetComponent<TrophyRoadFill>());
                int trophyCount = trophyRoadData.milestones[i].trophyRequirement;
                trophyRoadFills[i].trophyText.text = trophyRoadData.milestones[i].trophyRequirement.ToString();
            }
        }

        GenerateRewards();
        UpdateFillBars();
        LoadClaimedRewards();
        CheckForUnlockedRewards();
        UpdateRewardButtons();
        UpdatePointerPosition();
        Debug.Log("Loading :" + PlayerPrefs.GetInt("Trophies"));
        CheckForUnclaimedRewards();
    }
    public void GenerateEverything()
    {
        GenerateRewards();
        UpdateFillBars();
        LoadClaimedRewards();
        CheckForUnlockedRewards();
        UpdateRewardButtons();
        UpdatePointerPosition();
        CheckForUnclaimedRewards();
    }
    public int trophyRequirement, previousTrophyRequirement;
    public RectTransform fillBarRect;
    public float fillAmount;
    public TrophyRoadFill currentFillBar = null;
    private void UpdatePointerPosition()
    {
        int currentTrophies = PlayerPrefs.GetInt("Trophies", 0);


        for (int i = 1; i < trophyRoadFills.Count; i++) // Start from the second fill bar
        {
            if (currentTrophies < trophyRoadData.milestones[i].trophyRequirement)
            {
                currentFillBar = trophyRoadFills[i];
                Debug.Log("Current fill bar: " + currentFillBar.name);
                break;
            }
        }

        if (currentFillBar != null)
        {
            // Set the pointer as a child of the current fill bar
            pointer.transform.SetParent(currentFillBar.transform);

            fillBarRect = currentFillBar.fillBar.GetComponent<RectTransform>();

            // Calculate the pointer position within the current fill bar's local coordinates
            float pointerXPosition = Mathf.Lerp(
                fillBarRect.rect.xMin, // Local min x of the bar
                fillBarRect.rect.xMax, // Local max x of the bar
                currentFillBar.fillBar.fillAmount // fillAmount represents how filled the bar is
            );
            Debug.Log("Pointer x position: " + pointerXPosition);

            // Update pointer position within its parent's local space
            RectTransform pointerRect = pointer.GetComponent<RectTransform>();
            pointerRect.anchoredPosition = new Vector2(pointerXPosition, -74);

            // Update the pointer text
            pointerText.text = currentTrophies.ToString();
        }
    }
    private void SetTrophiesAtStart()
    {
        int trophies = PlayerPrefs.GetInt("Trophies", 0);
        pointerText.text = trophies.ToString();
        RectTransform pointerRect = pointer.GetComponent<RectTransform>();

        // Ensure the pointer starts at the beginning of the first bar
        pointerRect.anchoredPosition = new Vector2(0, pointerRect.anchoredPosition.y);
    }

    public void CheckForUnlockedRewards()
    {
        int currentTrophies = PlayerPrefs.GetInt("Trophies", 0);

        foreach (var milestone in trophyRoadData.milestones)
        {
            if (milestone.trophyRequirement <= currentTrophies && !IsRewardClaimed(milestone.trophyRequirement))
            {
                // Milestone is unlocked but not yet claimed
                Debug.Log($"Unlocked reward: {milestone.reward.description} at {milestone.trophyRequirement} trophies.");
                // Notify player or update UI
            }
        }
    }

    public void ClaimReward(int trophyRequirement, float delay)
    {
        if (!IsRewardClaimed(trophyRequirement))
        {
            claimedRewards.Add(trophyRequirement);
            GrantReward(trophyRoadData.milestones.Find(m => m.trophyRequirement == trophyRequirement).reward);
            SaveClaimedRewards();
            UpdateRewardButtons();
            CheckForUnclaimedRewards();
            if (trophyTutorialHint != null && trophyTutorialHintBack != null && trophyTutorialHint.activeSelf)
            {
                trophyTutorialHint.SetActive(false);
                StartCoroutine(EnableBackHint());
            }
        }
        StartCoroutine(ScrollToNextItem(delay));
    }
    public IEnumerator ScrollToNextItem(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("scrolling to item" + " " + itemIndex);
        customScrollTrophyRoad.CenterOnSelectedItem(itemIndex);

    }
    public IEnumerator EnableBackHint()
    {
        yield return new WaitForSeconds(1f);
        TutorialMenuManager tutorialMenuManager = FindObjectOfType<TutorialMenuManager>();
        if (tutorialMenuManager != null)
        {
            tutorialMenuManager.BackHint_03();
        }

    }

    private void GrantReward(TrophyRoadReward reward)
    { Debug.Log("Granting +Reward");
        // Implement logic to grant reward based on type
        switch (reward.rewardType)
        {
            case TrophyRewardType.Coins_Small or TrophyRewardType.Coins_Medium or TrophyRewardType.Coins_Large:
                Debug.Log($"Granted {reward.amount} coins.");
                AddCurrency("coins", reward.amount);
                break;
            case TrophyRewardType.Gems_Small or TrophyRewardType.Gems_Medium or TrophyRewardType.Gems_Large:
                Debug.Log($"Granted {reward.amount} gems.");
                AddCurrency("gems", reward.amount);
                break;
            case TrophyRewardType.Money_Small or TrophyRewardType.Money_Medium or TrophyRewardType.Money_Large:
                Debug.Log($"Granted {reward.amount} money.");
                AddCurrency("money", reward.amount);
                break;
            case TrophyRewardType.Character_Female:
                UnlockCharacter(CharacterType.Character_Female);
                break;
            case TrophyRewardType.Character_Red:
                UnlockCharacter(CharacterType.Character_Red);
                break;
            case TrophyRewardType.Character_Green:
                UnlockCharacter(CharacterType.Character_Green);
                break;
            case TrophyRewardType.Weapon_Pickaxe:
                UnlockWeapon(WeaponType.Weapon_Pickaxe);
                break;
            case TrophyRewardType.Weapon_Axe:
                UnlockWeapon(WeaponType.Weapon_Axe);
                break;
            case TrophyRewardType.Weapon_Bat:
                UnlockWeapon(WeaponType.Weapon_Bat);
                break;
            case TrophyRewardType.Character_Token:
                Debug.Log("Adding Tokens +" + reward.amount);
                AddCharacterToken(reward.amount);
                break;
            case TrophyRewardType.Chest_Currency:
                AddCurrency("coins", 300);
                AddCurrency("gems", 50);
                AddCurrency("money", 150);
                break;

            // Add more cases for other reward types
            default:
                Debug.LogWarning("Unknown reward type.");
                break;
        }
    }
    public void AddCharacterToken(int amount)
    {
        characterTokenManager.AddTokens(amount);
        // PlayerPrefs.SetInt("CharacterTokens", PlayerPrefs.GetInt("CharacterTokens", 0) + amount);
    }
    public void UnlockCharacter(CharacterType characterType)
    {
        PlayerPrefs.SetInt(characterType.ToString(), 1);
        CharacterSelector[] characterSelectors = FindObjectsOfType<CharacterSelector>(true);
        for (int i = 0; i < characterSelectors.Length; i++)
        {
            if (characterSelectors[i].characterType == characterType)
            {
                characterSelectors[i].LockCharacter(false);
            }
        }
    }
    public void UnlockWeapon(WeaponType weaponType)
    {
        PlayerPrefs.SetInt(weaponType.ToString(), 1);
        WeaponItem[] weaponItems = FindObjectsOfType<WeaponItem>(true);
        for (int i = 0; i < weaponItems.Length; i++)
        {
            if (weaponItems[i].weaponType == weaponType)
            {
                weaponItems[i].LockWeapon(false);
            }
        }
    }
    public void UnlockHelmet(HelmetType helmetType)
    {
        PlayerPrefs.SetInt(helmetType.ToString(), 1);
        HelmetItem[] helmetItems = FindObjectsOfType<HelmetItem>(true);
        for (int i = 0; i < helmetItems.Length; i++)
        {
            if (helmetItems[i].helmetType == helmetType)
            {
                helmetItems[i].LockHelmet(false);
            }
        }
    }
    public TextMeshProUGUI coinText, gemsText, moneyText;

    public void AddCurrency(string currency, int amount)
    {
        PlayerPrefs.SetInt(currency, PlayerPrefs.GetInt(currency) + amount);
        switch (currency)
        {
            case "coins":
                coinText.text = PlayerPrefs.GetInt(currency).ToString();
                break;
            case "gems":
                gemsText.text = PlayerPrefs.GetInt(currency).ToString();
                break;
            case "money":
                moneyText.text = PlayerPrefs.GetInt(currency).ToString();
                break;

        }
    }

    private bool IsRewardClaimed(int trophyRequirement)
    {
        return claimedRewards.Contains(trophyRequirement);
    }

    private void SaveClaimedRewards()
    {
        PlayerPrefs.SetString("ClaimedRewards", string.Join(",", claimedRewards));
        PlayerPrefs.Save();
    }

    private void LoadClaimedRewards()
    {
        string claimedRewardsString = PlayerPrefs.GetString("ClaimedRewards", string.Empty);
        if (!string.IsNullOrEmpty(claimedRewardsString))
        {
            claimedRewards = new List<int>(System.Array.ConvertAll(claimedRewardsString.Split(','), int.Parse));
        }
    }

    public void UpdateFillBars()
    {
        int currentTrophies = PlayerPrefs.GetInt("Trophies", 0);
        int previousTrophyRequirement;

        for (int i = 0; i < trophyRoadFills.Count; i++)
        {
            int trophyRequirement = trophyRoadData.milestones[i].trophyRequirement;
            previousTrophyRequirement = i > 0 ? trophyRoadData.milestones[i - 1].trophyRequirement : 0;

            if (currentTrophies >= trophyRequirement)
            {
                // If the player has surpassed this milestone, fill the bar completely
                trophyRoadFills[i].fillBar.fillAmount = 1f;
            }
            else
            {
                // Calculate the fill amount relative to this section's range
                int milestoneRange = trophyRequirement - previousTrophyRequirement;
                int trophiesInRange = currentTrophies - previousTrophyRequirement;
                float fillAmount = (float)trophiesInRange / milestoneRange;

                // Set the fill amount for this section
                trophyRoadFills[i].fillBar.fillAmount = fillAmount;

                // Stop after filling the current bar since the rest are unfilled
                break;
            }
        }
    }


    public Transform rewardButtonContainer;
    public Transform fillContainer;
    public GameObject rewardButtonPrefab, rewardButtonCharacterPrefab, trophyChestPrefab;
    public GameObject fillPrefab;
    public Sprite singleSprite;
    private void GenerateRewards()
    {
        foreach (var milestone in trophyRoadData.milestones)
        {
            GameObject rewardButtonObj;
            bool character;
            float width, height;
            if (milestone.reward.rewardType == TrophyRewardType.Chest_Currency)
            {
                rewardButtonObj = Instantiate(trophyChestPrefab, rewardButtonContainer);
                character = false;
                width = 150;
                height = 200;

            }
            else if (milestone.reward.rewardType == TrophyRewardType.Character_Female || milestone.reward.rewardType == TrophyRewardType.Character_Green || milestone.reward.rewardType == TrophyRewardType.Character_Red
            || milestone.reward.rewardType == TrophyRewardType.Weapon_Axe || milestone.reward.rewardType == TrophyRewardType.Weapon_Bat || milestone.reward.rewardType == TrophyRewardType.Weapon_Pickaxe || milestone.reward.rewardType == TrophyRewardType.Character_Token)
            {
                rewardButtonObj = Instantiate(rewardButtonCharacterPrefab, rewardButtonContainer);
                character = true;
                if (milestone.reward.rewardType == TrophyRewardType.Character_Token) character = false;
                width = 190;
                height = 250;
            }
            else
            {

                rewardButtonObj = Instantiate(rewardButtonPrefab, rewardButtonContainer);
                if (milestone.reward.rewardType == TrophyRewardType.Helmet_Bike || milestone.reward.rewardType == TrophyRewardType.Helmet_Rugby)
                {
                    character = true;
                }
                else character = false;

                width = 150;
                height = 150;
                Debug.Log("Instantiated :" + rewardButtonObj.name);
            }

            TrophyRoadButton rewardButton = rewardButtonObj.GetComponent<TrophyRoadButton>();
            // singleSprite = GetCurrencyIcon(milestone.reward.rewardType);


            rewardButton.Initialize(
                milestone.trophyRequirement,
                milestone.reward.amount.ToString(),
                milestone.reward.description,
                GetSpriteForRewardType(milestone.reward.rewardType),
                GetBackgroundForRewardType(milestone.reward.rewardType),
                this,
                trophyRoadPanel,
                character,
                milestone.reward.rewardType,
                trophyRoadData,
                GetAudioClipForSoundReward(milestone.reward.rewardType)
            );
            FitImageToBox(rewardButton.rewardImage, width, height);

            // FitImageToBox(rewardButton.smallCurrencyIcon, 185, 182);
        }

        // UpdateRewardButtons();
    }

    void FitImageToBox(Image image, float maxWidth, float maxHeight)
    {
        if (image.sprite == null) return;

        // Get the sprite's actual size
        float spriteWidth = image.sprite.rect.width;
        float spriteHeight = image.sprite.rect.height;

        // Calculate the scale factor to fit within the max dimensions
        float scaleFactor = Mathf.Min(maxWidth / spriteWidth, maxHeight / spriteHeight);

        // Set the RectTransform's sizeDelta to the scaled size
        image.rectTransform.sizeDelta = new Vector2(spriteWidth * scaleFactor, spriteHeight * scaleFactor);
        Debug.Log("Image width : " + spriteWidth + " Image height :" + spriteHeight);
    }
    private Sprite GetSpriteForRewardType(TrophyRewardType rewardType)
    {
        return rewardType switch
        {
            TrophyRewardType.Coins_Small => trophyRoadData.coinSprite_Small,
            TrophyRewardType.Gems_Small => trophyRoadData.gemSprite_Small,
            TrophyRewardType.Money_Small => trophyRoadData.moneySprite_Small,
            TrophyRewardType.Coins_Medium => trophyRoadData.coinSprite_Medium,
            TrophyRewardType.Gems_Medium => trophyRoadData.gemSprite_Medium,
            TrophyRewardType.Money_Medium => trophyRoadData.moneySprite_Medium,
            TrophyRewardType.Coins_Large => trophyRoadData.coinSprite_Large,
            TrophyRewardType.Gems_Large => trophyRoadData.gemSprite_Large,
            TrophyRewardType.Money_Large => trophyRoadData.moneySprite_Large,
            TrophyRewardType.Character_Female => trophyRoadData.character_Female,
            TrophyRewardType.Character_Red => trophyRoadData.character_Red,
            TrophyRewardType.Character_Green => trophyRoadData.character_Green,
            TrophyRewardType.Weapon_Pickaxe => trophyRoadData.weapon_Pickaxe,
            TrophyRewardType.Weapon_Axe => trophyRoadData.weapon_Axe,
            TrophyRewardType.Weapon_Bat => trophyRoadData.weapon_Bat,
            TrophyRewardType.Chest_Currency => trophyRoadData.chestSprite,
            TrophyRewardType.Helmet_Bike => trophyRoadData.helmet_Bike,
            TrophyRewardType.Helmet_Rugby => trophyRoadData.helmet_Rugby,
            TrophyRewardType.Character_Token => trophyRoadData.characterTokenSprite,

            _ => null,
        };
    }
    private AudioClip GetAudioClipForSoundReward(TrophyRewardType rewardType)
    {
        return rewardType switch
        {
            TrophyRewardType.Coins_Small or TrophyRewardType.Coins_Medium or TrophyRewardType.Coins_Large => trophyRoadData.audioClipGold,
            TrophyRewardType.Gems_Small or TrophyRewardType.Gems_Medium or TrophyRewardType.Gems_Large => trophyRoadData.audioClipGems,
            TrophyRewardType.Money_Small or TrophyRewardType.Money_Medium or TrophyRewardType.Money_Large => trophyRoadData.audioClipGold,
            TrophyRewardType.Chest_Currency or TrophyRewardType.Character_Token => trophyRoadData.audioClipChest,

            _ => null,
        };
    }
    public TrophyRoadButton firstClaimableReward;
    public int itemIndex;
    private bool isSet;
    public void UpdateRewardButtons()
    {
        if (!isSet)
        {
            Debug.Log("Reward not set");
            firstClaimableReward = null;
            itemIndex = 0;
        }
        Debug.Log("Calling update");
        TrophyRoadButton[] rewardButtons = rewardButtonContainer.GetComponentsInChildren<TrophyRoadButton>();
        int currentTrophies = PlayerPrefs.GetInt("Trophies", 0);

        foreach (var button in rewardButtons)
        {

            bool isClaimable = button.TrophyRequirement <= currentTrophies;
            bool isClaimed = IsRewardClaimed(button.TrophyRequirement);
            Debug.Log("Is claimable : " + isClaimable + " Is claimed :" + isClaimed + "First claimable reward" + firstClaimableReward + "is set? :" + isSet);
            if (isClaimable && !isClaimed && firstClaimableReward == null && !isSet)
            {
                firstClaimableReward = button;
                isSet = true;
                itemIndex = button.transform.GetSiblingIndex();

                Debug.Log("First unclaimed reward is :" + firstClaimableReward.name + " At index :" + button.transform.GetSiblingIndex());
            }
            button.SetButtonState(isClaimable, isClaimed);

        }
    }

    public Image backgroundImage;
    public Sprite claimableImage, unclaimableImage;
    public List<TrophyRoadMilestone> unclaimedMilestones = new List<TrophyRoadMilestone>();
    private bool nextUnclaimedRewardSet;
    // private void CheckForUnclaimedRewards()
    // {
    //     int tempIndex = 0;
    //     int currentTrophies = PlayerPrefs.GetInt("Trophies", 0);
    //     bool hasUnclaimedRewards = false;

    //     foreach (var milestone in trophyRoadData.milestones)
    //     {
    //         tempIndex++;
    //         if (milestone.trophyRequirement <= currentTrophies && !IsRewardClaimed(milestone.trophyRequirement))
    //         {
    //             unclaimedMilestones.Add(milestone);
    //             Debug.Log($"There is an unclaimed reward for {milestone.reward.description} at {milestone.trophyRequirement} trophies.");
    //             hasUnclaimedRewards = true;
    //             if (!nextUnclaimedRewardSet)
    //             {

    //                 itemIndex = tempIndex - 1;
    //                 if (itemIndex < 0) itemIndex = 0;
    //                 nextUnclaimedRewardSet = true;
    //                 Debug.Log("Setting index to : " + itemIndex);
    //             }

    //         }
    //     }

    //     // Update the background image based on whether there are unclaimed rewards
    //     backgroundImage.sprite = hasUnclaimedRewards ? claimableImage : unclaimableImage;
    //     exclamationMark.SetActive(hasUnclaimedRewards);
    //     nextUnclaimedRewardSet = false;
    // }
    private void CheckForUnclaimedRewards()
    {
        int tempIndex = 0;
        int currentTrophies = PlayerPrefs.GetInt("Trophies", 0);
        bool hasUnclaimedRewards = false;
        TrophyRoadMilestone nextUnavailableMilestone = null;

        unclaimedMilestones.Clear();
        nextUnclaimedRewardSet = false;

        foreach (var milestone in trophyRoadData.milestones)
        {
            if (milestone.trophyRequirement <= currentTrophies && !IsRewardClaimed(milestone.trophyRequirement))
            {
                // Found an unclaimed and claimable reward
                unclaimedMilestones.Add(milestone);
                hasUnclaimedRewards = true;

                if (!nextUnclaimedRewardSet)
                {
                    // Set the index to the first unclaimed and claimable reward
                    itemIndex = tempIndex;
                    nextUnclaimedRewardSet = true;
                }
            }

            // Track the first unavailable and unclaimed milestone as fallback
            if (milestone.trophyRequirement > currentTrophies && nextUnavailableMilestone == null && !IsRewardClaimed(milestone.trophyRequirement))
            {
                nextUnavailableMilestone = milestone;
                if (!nextUnclaimedRewardSet) // Only set the fallback index if no unclaimed rewards are found
                {
                    itemIndex = tempIndex;
                }
            }

            tempIndex++;
        }

        // Update the background image and exclamation mark
        backgroundImage.sprite = hasUnclaimedRewards ? claimableImage : unclaimableImage;
        exclamationMark.SetActive(hasUnclaimedRewards);

        // Debug logging
        if (hasUnclaimedRewards)
        {
            Debug.Log($"First unclaimed reward set to index: {itemIndex}");
        }
        else if (nextUnavailableMilestone != null)
        {
            Debug.Log($"No claimable rewards. Next reward to be claimed is: {nextUnavailableMilestone.reward.description} at index: {itemIndex}");
        }
        else
        {
            Debug.Log("No claimable or future rewards found.");
        }
    }

    private Sprite GetBackgroundForRewardType(TrophyRewardType rewardType)
    {
        return rewardType switch
        {
            TrophyRewardType.Coins_Small or TrophyRewardType.Coins_Medium or TrophyRewardType.Coins_Large => trophyRoadData.backgroundYellow,
            TrophyRewardType.Gems_Small or TrophyRewardType.Gems_Medium or TrophyRewardType.Gems_Large => trophyRoadData.backgroundPink,
            TrophyRewardType.Money_Small or TrophyRewardType.Money_Medium or TrophyRewardType.Money_Large => trophyRoadData.backgroundGreen,
            _ => trophyRoadData.backgroundBlue,
        };
    }
}