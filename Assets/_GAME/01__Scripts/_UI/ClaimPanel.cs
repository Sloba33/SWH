using UnityEngine.UI;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using Bitgem.Core;
using System.Collections.Generic;
using System;
public class ClaimPanel : MonoBehaviour
{
    public GameObject coins, gems, money;
    public GameObject glow;
    public Image itemImage;
    public Sprite characterPortraitImage;
    public GameObject chestObject;
    public Image background;
    public TextMeshProUGUI itemText;
    public Button button;
    public Transform tweenParent;
    public GameObject currencyPanel, tokenPanel, xpPanel;

    public ClaimPanelCurrencyGatherer claimPanelCurrencyGatherer;
    private void Start()
    {
        Debug.Log("Spawning panel");
        tweenParent = itemImage.transform.parent;
        button.onClick.AddListener(CloseWindow);
        StartCoroutine(TweenObject());

    }
    public void SetTextAndImage(string text, Sprite itemSprite, Sprite bgSprite)
    {


        background.sprite = bgSprite;
        itemText.text = text;
        itemImage.sprite = itemSprite;
        Debug.Log("itemimage name:" + itemSprite.name);
        if (itemSprite.name.Contains("Coin"))
        {
            Debug.Log("Activating coins");
            coins.SetActive(true);
        }
        else if (itemSprite.name.Contains("Gem"))
        {
            gems.SetActive(true);
        }
        else if (itemSprite.name.Contains("Money"))
        {

            money.SetActive(true);
        }

    }
    public void SpawnCurrencyGatherer(ImageGalleryDataSO.RewardData reward)
    {
        claimPanelCurrencyGatherer.gameObject.SetActive(true);
        claimPanelCurrencyGatherer.Initialize(reward.rewardType);
    }
    private void AddCurrency(String currency, int amount)
    {

        Debug.Log("Currency :" + currency + "was :" + PlayerPrefs.GetInt(currency));

        PlayerPrefs.SetInt(currency, PlayerPrefs.GetInt(currency) + amount);
        Debug.Log("Currency :" + currency + "now is :" + PlayerPrefs.GetInt(currency));
        switch (currency)
        {
            case "coins":
                UI_Manager.Instance.coinText.text = PlayerPrefs.GetInt("coins").ToString();
                coins.SetActive(true);
                break;
            case "money":
                UI_Manager.Instance.cashText.text = PlayerPrefs.GetInt("money").ToString();
                money.SetActive(true);
                break;
            case "gems":
                UI_Manager.Instance.gemText.text = PlayerPrefs.GetInt("gems").ToString();
                gems.SetActive(true);
                break;
        }

    }
    public void SetTextAndImageCharacter(string text, Sprite itemSprite, Sprite bgSprite)
    {
        background.sprite = bgSprite;
        itemText.text = "";
        itemImage.sprite = characterPortraitImage;

    }
    public void SetTextAndImage(string text, Sprite itemSprite, Sprite bgSprite, List<ClaimPanel> claimpanels)
    {
        background.sprite = bgSprite;
        itemText.text = text;
        itemImage.sprite = itemSprite;

    }
    public void SetRewardData(ImageGalleryDataSO.RewardData reward)
    {
        if (reward == null)
        {
            Debug.LogError("RewardData is null.");
            return;
        }
        Debug.Log("Setting reward Data");
        background.sprite = reward.backgroundSprite;
        itemText.text = reward.amount.ToString(); // Assuming amount is what you want to display
        itemImage.sprite = reward.rewardSprite;
        AddCurrency(reward);
        // SpawnCurrencyGatherer(reward);
    }
    private void SetCurrencyText()
    {

    }
    public void AddCurrency(ImageGalleryDataSO.RewardData reward)
    {
        switch (reward)
        {
            case ImageGalleryDataSO.RewardData rd when rd.rewardType == ImageGalleryDataSO.RewardType.Coins:
                AddCurrency("coins", reward.amount);
                break;
            case ImageGalleryDataSO.RewardData rd when rd.rewardType == ImageGalleryDataSO.RewardType.Cash:
                AddCurrency("money", reward.amount);
                break;
            case ImageGalleryDataSO.RewardData rd when rd.rewardType == ImageGalleryDataSO.RewardType.Gems:
                AddCurrency("diamonds", reward.amount);
                break;
            case ImageGalleryDataSO.RewardData rd when rd.rewardType == ImageGalleryDataSO.RewardType.Token:
                AddCurrency("tokens", reward.amount);
                break;
            case ImageGalleryDataSO.RewardData rd when rd.rewardType == ImageGalleryDataSO.RewardType.XP:
                AddCurrency("xp", reward.amount);
                break;
        }
    }
    public IEnumerator TweenObject()
    {
        yield return new WaitForSeconds(0.5f);
        FitImageToBox(itemImage, 640, 640);
        tweenParent.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        tweenParent.gameObject.SetActive(true);
        // tweenParent.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.5f, 5, 0.4f).Play();
        tweenParent.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.4f).Play();
        tweenParent.DOScale(new Vector3(0.8f, 0.8f, 0.8f), 0.5f).SetDelay(0.45f).Play();
        yield return new WaitForSeconds(0.35f);
        glow.SetActive(true);
        yield return new WaitForSeconds(0.4f);
        button.gameObject.SetActive(true);
        button.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        button.transform.DOScale(new Vector3(1, 1, 1), 0.2f).Play();
    }
    public void CloseWindow()
    {
        Destroy(this.gameObject);
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

}
