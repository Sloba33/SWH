using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "DailyRewardData", menuName = "DailyReward/DailyRewardData", order = 1)]
public class DailyRewardData : ScriptableObject
{
    public List<DailyLoginReward> dailyLoginRewards;
    public Sprite coinSprite_Small, coinSprite_Medium, coinSprite_Large;
    public Sprite gemSprite_Small, gemSprite_Medium, gemSprite_Large;
    public Sprite moneySprite_Small, moneySprite_Medium, moneySprite_Large;
    public Sprite backgroundBlue, backgroundGreen, backgroundPink, backgroundYellow;
    public Sprite singleCoin, singleGem, singleMoney;
}
public enum DailyRewardType
{
    Coins_Small, Coins_Medium, Coins_Large,
    Gems_Small, Gems_Medium, Gems_Large,
    Money_Small, Money_Medium, Money_Large
}

[System.Serializable]
public class DailyLoginReward
{
    public DailyRewardType dailyRewardType;
    public int amount;
    public string description;
}