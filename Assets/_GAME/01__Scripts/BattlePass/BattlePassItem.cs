using UnityEngine;

[System.Serializable]
public enum RewardType
{
    Coins_Small,
    Gems_Small,
    Money_Small,
    Coins_Medium,
    Gems_Medium,
    Money_Medium,
    Coins_Large,
    Gems_Large,
    Money_Large
}

[System.Serializable]
public class BattlePassItem
{
    public RewardType rewardType;
    public int amount; // Amount of the reward
    public string description; // Optional description

}
