using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class ClaimPanelCurrencyGatherer : MonoBehaviour
{

    public GameObject cashPanel, coinPanel, gemPanel, xpPane, tokenPanel;
    public CurrencyType currencyType;
    public void Initialize(ImageGalleryDataSO.RewardType rewardType)
    {
        switch (rewardType)
        {
            case ImageGalleryDataSO.RewardType.Coins:
                currencyType = CurrencyType.Coins;
                break;
            case ImageGalleryDataSO.RewardType.Cash:
                currencyType = CurrencyType.Cash;
                break;
            case ImageGalleryDataSO.RewardType.Gems:
                currencyType = CurrencyType.Gems;
                break;
            case ImageGalleryDataSO.RewardType.Token:
                currencyType = CurrencyType.Tokens;
                break;
            case ImageGalleryDataSO.RewardType.XP:
                currencyType = CurrencyType.XP;
                break;
        }
    }
    public void ActivatePanel(GameObject obj)
    {
        obj.SetActive(true);
    }
}

public enum CurrencyType
{
    Coins,
    Cash,
    Gems,
    Tokens,
    XP
}
