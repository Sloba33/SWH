using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyReward : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timerText;

    public Image rewardImage;
    public DailyRewardManager manager;
    private Sprite backgroundSprite;
    public ClaimPanel claimPanel;
    private Transform parent;
    public bool isCurrent;
    public DateTime nextClaimTime;
    TimeSpan remainingTime;

    public void Initialize(string amount, Sprite rewardSprite, Sprite bgSprite, DailyRewardManager manager, Transform parentPanel)
    {
        amountText.text = amount;
        rewardImage.sprite = rewardSprite;
        this.manager = manager;
        backgroundSprite = bgSprite;
        button.onClick.AddListener(OnClaimButtonClick);
        parent = parentPanel;

    }
    private void OnClaimButtonClick()
    {
        manager.ClaimReward();
        rewardImage.transform.DOShakeRotation(0.65f).Play();

        StartCoroutine(SpawnPanel());
    }
    public IEnumerator SpawnPanel()
    {
        yield return new WaitForSeconds(0.5f);
        ClaimPanel cPanel = Instantiate(claimPanel, parent);
        cPanel.SetTextAndImage("x" + amountText.text, rewardImage.sprite, backgroundSprite);
    }
    public void SetButtonState(bool isClaimable, bool isClaimed)
    {
        if (!isClaimable && isClaimed)
        {
            SetButtonClaimed();

        }
        else if (isClaimable && !isClaimed)
        {
            SetButtonAsUnclaimedAndAvailable();
        }
        else if (!isClaimable && !isClaimed)
        {
            SetButtonAsUnclaimedAndUnavailable();
        }
        else Debug.Log("How the f");


    }

    public void SetButtonClaimed()
    {
        Color faded = new(1, 1, 1, 0.5f);
        button.GetComponent<Image>().color = faded;
        button.interactable = false;
        Debug.Log("Daily reward has already been claimed :" + this.name);
    }
    public void SetButtonAsUnclaimedAndAvailable()
    {
        Debug.Log("Button set To Unclaimable and Available :" + this.name);
        button.interactable = true;
    }
    public void SetButtonAsUnclaimedAndUnavailable()
    {
        Debug.Log("Button set to Unclaimed and Unavailable:" + this.name);
        button.interactable = false;

    }

}
