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
        GenerateRewards();
        UpdateFillBars();
        LoadClaimedRewards();
        CheckForUnlockedRewards();
        UpdateRewardButtons();
        UpdatePointerPosition();
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


        for (int i = 1; i < trophyRoadFills.Count; i++)
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

            pointer.transform.SetParent(currentFillBar.transform);

            fillBarRect = currentFillBar.fillBar.GetComponent<RectTransform>();

            float pointerXPosition = Mathf.Lerp(
                fillBarRect.rect.xMin,
                fillBarRect.rect.xMax,
                currentFillBar.fillBar.fillAmount
            );



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
        pointerRect.anchoredPosition = new Vector2(0, pointerRect.anchoredPosition.y);
    }

    public void CheckForUnlockedRewards()
    {
        int currentTrophies = PlayerPrefs.GetInt("Trophies", 0);

        foreach (var milestone in trophyRoadData.milestones)
        {
            if (milestone.trophyRequirement <= currentTrophies && !IsRewardClaimed(milestone.trophyRequirement))
            {

                Debug.Log($"Unlocked reward: {milestone.reward.description} at {milestone.trophyRequirement} trophies.");

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

    }
    private void GrantReward(TrophyRoadReward reward)
    {
        Debug.Log("Granting +Reward in TR");


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
            case TrophyRewardType.Weapon_Hammer:
                UnlockWeapon(WeaponType.Weapon_Hammer);
                break;
            case TrophyRewardType.Weapon_Hammer2:
                UnlockWeapon(WeaponType.Weapon_Hammer2);
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


            default:
                Debug.LogWarning("Unknown reward type.");
                break;
        }
    }
    public void AddCharacterToken(int amount)
    {
        characterTokenManager.AddTokens(amount);

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
        Debug.Log("Unlocking weapon :" + weaponType);
        PlayerPrefs.SetInt(weaponType.ToString(), 1);

        WeaponItem[] weaponItems = FindObjectsOfType<WeaponItem>(true);
        for (int i = 0; i < weaponItems.Length; i++)
        {
            if (weaponItems[i].weaponType == weaponType)
            {
                if (PlayerPrefs.GetInt("AnyWeaponsUnlocked") == 0)
                    CharacterManager.Instance.weaponItem = weaponItems[i];
                weaponItems[i].LockWeapon(false);
                PlayerPrefs.SetInt("AnyWeaponsUnlocked", 1);
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

                trophyRoadFills[i].fillBar.fillAmount = 1f;
            }
            else
            {

                int milestoneRange = trophyRequirement - previousTrophyRequirement;
                int trophiesInRange = currentTrophies - previousTrophyRequirement;
                float fillAmount = (float)trophiesInRange / milestoneRange;


                trophyRoadFills[i].fillBar.fillAmount = fillAmount;

                break;
            }
        }
    }


    public Transform rewardButtonContainer;
    public Transform fillContainer;
    public Transform fillBarPanel;
    public Transform rewardsPanel;
    public GameObject rewardButtonPrefab, rewardButtonCharacterPrefab, trophyChestPrefab;
    public GameObject fillPrefab_130px, fillPrefab_500px;
    public Sprite singleSprite;
    public Transform trophyRoadContainer; // assign Content here
    public void UpdateGridContentWidth(GridLayoutGroup grid, RectTransform contentRect, RectTransform rewardsPanel)
    {
        if (grid == null || contentRect == null)
        {
            Debug.LogWarning("UpdateGridContentWidth: grid or contentRect is null.");
            return;
        }

        // Make sure layout values are up to date
        LayoutRebuilder.ForceRebuildLayoutImmediate(grid.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        int totalChildren = rewardsPanel.transform.childCount;
        if (totalChildren == 0)
        {
            // nothing instantiated yet
            contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);
            return;
        }

        // Rows (we expect Fixed Row Count == 2)
        int rows = 2;
        if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            rows = Mathf.Max(1, grid.constraintCount);
        else if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            rows = 1; // if you constrained columns this logic would change

        int columns = Mathf.CeilToInt((float)totalChildren / rows);

        float cellWidth = grid.cellSize.x;
        float spacingX = grid.spacing.x;
        float paddingLR = grid.padding.left + grid.padding.right;

        float requiredWidth = columns * cellWidth + Mathf.Max(0, columns - 1) * spacingX + paddingLR;

        // Keep current height
        contentRect.sizeDelta = new Vector2(requiredWidth, contentRect.sizeDelta.y);
        grid.cellSize = new Vector2(totalChildren * grid.cellSize.x, grid.cellSize.y);

        // Debug
        Debug.Log($"Grid children: {totalChildren}, rows: {rows}, columns: {columns}, cellWidth: {cellWidth}, requiredContentWidth: {requiredWidth}");
    }
    private void GenerateRewards()
    {

        foreach (Transform child in rewardsPanel)
            Destroy(child.gameObject);
        foreach (Transform child in fillBarPanel)
            Destroy(child.gameObject);
        trophyRoadFills.Clear();

        for (int i = 0; i < trophyRoadData.milestones.Count; i++)
        {
            TrophyRoadMilestone milestone = trophyRoadData.milestones[i];


            GameObject fillPrefabToUse = (i == 0) ? fillPrefab_130px : fillPrefab_500px;
            GameObject fillObj = Instantiate(fillPrefabToUse, fillBarPanel);
            fillObj.name = $"Fill_{milestone.trophyRequirement}_{i}";

            TrophyRoadFill fill = fillObj.GetComponent<TrophyRoadFill>();
            if (fill != null)
            {
                trophyRoadFills.Add(fill);
                fill.trophyText.text = milestone.trophyRequirement.ToString();
            }


            GameObject rewardObj = CreateRewardObject(milestone);
            rewardObj.transform.SetParent(rewardsPanel, false);
            rewardObj.name = $"Reward_{milestone.trophyRequirement}_{i}";
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(fillContainer.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(rewardsPanel.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(fillBarPanel.GetComponent<RectTransform>());
        UpdateGridContentWidth(fillContainer.GetComponent<GridLayoutGroup>(), fillContainer.GetComponent<RectTransform>(), rewardsPanel.GetComponent<RectTransform>());
    }
    private GameObject CreateRewardObject(TrophyRoadMilestone milestone)
    {
        GameObject rewardButtonObj = null;
        bool character = false;
        float width = 150f, height = 150f;

        TrophyRewardType type = milestone.reward.rewardType;

        if (type == TrophyRewardType.Chest_Currency)
        {
            rewardButtonObj = Instantiate(trophyChestPrefab);
            character = false;
            width = 150f;
            height = 200f;
        }
        else if (type == TrophyRewardType.Character_Female
              || type == TrophyRewardType.Character_Green
              || type == TrophyRewardType.Character_Red
              || type == TrophyRewardType.Weapon_Axe
              || type == TrophyRewardType.Weapon_Bat
              || type == TrophyRewardType.Weapon_Pickaxe
              || type == TrophyRewardType.Character_Token
              || type == TrophyRewardType.Weapon_Hammer2
              || type == TrophyRewardType.Weapon_Hammer)
        {
            rewardButtonObj = Instantiate(rewardButtonCharacterPrefab);

            character = (type != TrophyRewardType.Character_Token);
            width = 190f;
            height = 111f;
        }
        else
        {
            rewardButtonObj = Instantiate(rewardButtonPrefab);

            character = (type == TrophyRewardType.Helmet_Bike || type == TrophyRewardType.Helmet_Rugby);
            width = 150f;
            height = 150f;
        }

        if (rewardButtonObj == null)
        {
            Debug.LogError("Failed to instantiate reward prefab. Check prefab references.");
            return null;
        }

        rewardButtonObj.name = $"Reward_{milestone.trophyRequirement}_{type}";

        TrophyRoadButton rewardButton = rewardButtonObj.GetComponent<TrophyRoadButton>();
        if (rewardButton == null)
        {
            Debug.LogWarning("Reward prefab missing TrophyRoadButton component.");
            return rewardButtonObj;
        }


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

        return rewardButtonObj;
    }

    void FitImageToBox(Image image, float maxWidth, float maxHeight)
    {
        if (image.sprite == null) return;

        float spriteWidth = image.sprite.rect.width;
        float spriteHeight = image.sprite.rect.height;

        float scaleFactor = Mathf.Min(maxWidth / spriteWidth, maxHeight / spriteHeight);

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
            TrophyRewardType.Weapon_Hammer => trophyRoadData.weapon_Hammer,
            TrophyRewardType.Weapon_Hammer2 => trophyRoadData.weapon_Hammer2,

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
    public void CheckIfWeaponIsUnlocked()
    {
        if (PlayerPrefs.GetInt("AnyWeaponsUnlocked") == 1)
        {
            CharacterManager.Instance.EnableBoxWeapon();
        }

    }
    public Image backgroundImage;
    public Sprite claimableImage, unclaimableImage;
    public List<TrophyRoadMilestone> unclaimedMilestones = new List<TrophyRoadMilestone>();
    private bool nextUnclaimedRewardSet;
  
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
                
                unclaimedMilestones.Add(milestone);
                hasUnclaimedRewards = true;

                if (!nextUnclaimedRewardSet)
                {
          
                    itemIndex = tempIndex;
                    nextUnclaimedRewardSet = true;
                }
            }

           
            if (milestone.trophyRequirement > currentTrophies && nextUnavailableMilestone == null && !IsRewardClaimed(milestone.trophyRequirement))
            {
                nextUnavailableMilestone = milestone;
                if (!nextUnclaimedRewardSet)
                {
                    itemIndex = tempIndex;
                }
            }

            tempIndex++;
        }

    
        backgroundImage.sprite = hasUnclaimedRewards ? claimableImage : unclaimableImage;
        exclamationMark.SetActive(hasUnclaimedRewards);

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