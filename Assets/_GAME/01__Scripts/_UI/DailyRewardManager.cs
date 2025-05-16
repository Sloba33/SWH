using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardManager : MonoBehaviour
{
    [SerializeField] public DailyRewardData dailyRewardData;
    [SerializeField] private GameObject rewardPrefab; // Prefab with DailyReward component
    [SerializeField] private Transform rewardsContainer; // Parent object to hold instantiated rewards
    [SerializeField] private TMPro.TextMeshProUGUI timerText;
    [SerializeField] private Transform parentPanel;
    public DailyReward currentReward;
    private int currentRewardIndex;
    private DateTime lastClaimTime, nextClaimTime;
    private const string LastClaimTimeKey = "LastClaimTime";
    private const string NextClaimTimeKey = "NextClaimTime";
    private const string CurrentRewardIndexKey = "CurrentRewardIndex";
    private TimeSpan timeUntilNextClaim;
    public List<DailyReward> dailyRewards = new();

    [SerializeField] private TMPro.TextMeshProUGUI moneyText;
    [SerializeField] private TMPro.TextMeshProUGUI coinText;
    [SerializeField] private TMPro.TextMeshProUGUI gemsText;
    void Start()
    {
        LoadProgress();

        GenerateRewards();
        UpdateTimer();
    }

    void Update()
    {
        UpdateTimer();
    }

    private void GenerateRewards()
    {
        for (int i = 0; i < dailyRewardData.dailyLoginRewards.Count; i++)
        {
            GameObject rewardInstance = Instantiate(rewardPrefab, rewardsContainer);
            DailyReward rewardComponent = rewardInstance.GetComponent<DailyReward>();
            DailyLoginReward rewardData = dailyRewardData.dailyLoginRewards[i];

            // Initialize reward component with the data
            rewardComponent.Initialize(
            rewardData.amount.ToString(),
            GetSpriteForRewardType(rewardData.dailyRewardType),
            GetBackgroundForRewardType(rewardData.dailyRewardType),
            this,
            parentPanel);
            UpdateRewardButtons();
            rewardInstance.name = rewardInstance.name + i.ToString();
            Debug.Log("Instantiating object with name : " + rewardInstance.name);
        }

    }
    public DailyReward[] rewardButtons;
    public Button currentButton;
    public void UpdateRewardButtons()
    {
        rewardButtons = rewardsContainer.GetComponentsInChildren<DailyReward>();
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            Debug.Log("Daily Reward - IsClaimable = " + (i == currentRewardIndex && DateTime.Now >= nextClaimTime));
            Debug.Log("Daily Reward - IsClaimed = " + IsRewardClaimed(i));
            Debug.Log("Daily reward DATETIME : " + (DateTime.Now >= nextClaimTime));
            bool isClaimable = i == currentRewardIndex && DateTime.Now >= nextClaimTime;
            bool isClaimed = IsRewardClaimed(i);
            if (isClaimed)
            {
                Debug.Log("Setting claimable to false:");
                isClaimable = false;
            }
            rewardButtons[i].SetButtonState(isClaimable, isClaimed);
            if (i == currentRewardIndex)
            {
                currentReward = rewardButtons[currentRewardIndex];
                currentReward.isCurrent = true;
                currentReward.timerText.gameObject.SetActive(true);
                currentButton = currentReward.GetComponent<Button>();
                currentButton.interactable = DateTime.Now >= nextClaimTime;
            }
            else
            {
                rewardButtons[i].isCurrent = false;
                rewardButtons[i].timerText.gameObject.SetActive(false);
            }

        }
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
    public bool IsRewardClaimed(int index)
    {
        if (index < currentRewardIndex) return true;
        else return false;
    }
    private void LoadProgress()
    {
        // Load the last claim time and current reward index
        string lastClaimTimeString = PlayerPrefs.GetString(LastClaimTimeKey, DateTime.MinValue.ToString());
        string nextClaimTimeString = PlayerPrefs.GetString(NextClaimTimeKey, DateTime.MinValue.ToString());
        lastClaimTime = DateTime.Parse(lastClaimTimeString);
        nextClaimTime = DateTime.Parse(nextClaimTimeString);
        currentRewardIndex = PlayerPrefs.GetInt(CurrentRewardIndexKey, 0);
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetString(LastClaimTimeKey, lastClaimTime.ToString());
        string lastClaimTimeString = PlayerPrefs.GetString(LastClaimTimeKey, DateTime.MinValue.ToString());
        DateTime lastClaimTimeTimeDate = DateTime.Parse(lastClaimTimeString);
        DateTime nextDay = lastClaimTime.AddDays(1);
        nextClaimTime = nextDay.Date;
        Debug.Log("Time till next claim " + (nextClaimTime - DateTime.Now).ToString());
        PlayerPrefs.SetString(NextClaimTimeKey, nextClaimTime.ToString());
        PlayerPrefs.SetInt(CurrentRewardIndexKey, currentRewardIndex);
        PlayerPrefs.Save();
    }




    public void ClaimReward()
    {
        if (!IsRewardClaimed(currentRewardIndex))
        {

            lastClaimTime = DateTime.Now;
            GrantReward(dailyRewardData.dailyLoginRewards[currentRewardIndex]);
            rewardButtons[currentRewardIndex].timerText.gameObject.SetActive(false);
            rewardButtons[currentRewardIndex].SetButtonClaimed();
            currentRewardIndex++;
            UpdateRewardButtons();
            SaveProgress();
        }

        // Highlight the next reward the player can claim
        if (currentRewardIndex < dailyRewardData.dailyLoginRewards.Count)
        {
            Debug.Log("More shit to claim");
            // Assuming that the reward prefab has a way to highlight or mark the next claimable reward
            Transform nextReward = rewardsContainer.GetChild(currentRewardIndex);
            DailyReward nextDailyReward = nextReward.GetComponent<DailyReward>();
            if (nextDailyReward != null)
            {
                nextDailyReward.nextClaimTime = DateTime.Parse(PlayerPrefs.GetString(NextClaimTimeKey, DateTime.MinValue.ToString()));
                nextDailyReward.timerText.text = (nextDailyReward.nextClaimTime - DateTime.Now).ToString();
            }
            // nextReward.GetComponent<DailyReward>().SetClaimable(true);
        }
    }
    TimeSpan remainingTime;
    private void UpdateTimer()
    {
        if (!currentReward.isCurrent) return;
        remainingTime = nextClaimTime - DateTime.Now;
        // Debug.Log("Remaining time " + remainingTime + "Timespan ZERO " + TimeSpan.Zero + "Flag :" + (remainingTime > TimeSpan.Zero));
        if (remainingTime <= TimeSpan.Zero)
        {
            currentButton.interactable = true;
            currentReward.timerText.text = "Available Now!";
            // Debug.Log("WE ENABLING IT");

        }
        else
        {
            currentButton.interactable = false;
            currentReward.timerText.text = remainingTime.Hours + "h " + remainingTime.Minutes + "m";
        }
    }
    private Sprite GetSpriteForRewardType(DailyRewardType rewardType)
    {
        return rewardType switch
        {
            DailyRewardType.Coins_Small => dailyRewardData.coinSprite_Small,
            DailyRewardType.Gems_Small => dailyRewardData.gemSprite_Small,
            DailyRewardType.Money_Small => dailyRewardData.moneySprite_Small,
            DailyRewardType.Coins_Medium => dailyRewardData.coinSprite_Medium,
            DailyRewardType.Gems_Medium => dailyRewardData.gemSprite_Medium,
            DailyRewardType.Money_Medium => dailyRewardData.moneySprite_Medium,
            DailyRewardType.Coins_Large => dailyRewardData.coinSprite_Large,
            DailyRewardType.Gems_Large => dailyRewardData.gemSprite_Large,
            DailyRewardType.Money_Large => dailyRewardData.moneySprite_Large,
            _ => null,
        };
    }
    private Sprite GetBackgroundForRewardType(DailyRewardType rewardType)
    {
        return rewardType switch
        {
            DailyRewardType.Coins_Small or DailyRewardType.Coins_Medium or DailyRewardType.Coins_Large => dailyRewardData.backgroundYellow,
            DailyRewardType.Gems_Small or DailyRewardType.Gems_Medium or DailyRewardType.Gems_Large => dailyRewardData.backgroundPink,
            DailyRewardType.Money_Small or DailyRewardType.Money_Medium or DailyRewardType.Money_Large => dailyRewardData.backgroundGreen,
            _ => null,
        };
    }
    private void GrantReward(DailyLoginReward reward)
    {
        // Implement logic to grant reward based on type
        switch (reward.dailyRewardType)
        {
            case DailyRewardType.Coins_Small or DailyRewardType.Coins_Medium or DailyRewardType.Coins_Large:
                Debug.Log($"Granted {reward.amount} coins.");
                AddCurrency("coins", reward.amount);
                break;
            case DailyRewardType.Gems_Small or DailyRewardType.Gems_Medium or DailyRewardType.Gems_Large:
                Debug.Log($"Granted {reward.amount} gems.");
                AddCurrency("gems", reward.amount);
                break;
            case DailyRewardType.Money_Small or DailyRewardType.Money_Medium or DailyRewardType.Money_Large:
                Debug.Log($"Granted {reward.amount} money.");
                AddCurrency("money", reward.amount);
                break;
            // Add more cases for other reward types
            default:
                Debug.LogWarning("Unknown reward type.");
                break;
        }
    }
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
}
