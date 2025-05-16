using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrophyRoadData", menuName = "TrophyRoad/TrophyRoadData", order = 1)]
public class TrophyRoadData : ScriptableObject
{
    public List<TrophyRoadMilestone> milestones;
    public Sprite coinSprite_Small, coinSprite_Medium, coinSprite_Large;
    public Sprite gemSprite_Small, gemSprite_Medium, gemSprite_Large;
    public Sprite moneySprite_Small, moneySprite_Medium, moneySprite_Large;
    public Sprite backgroundBlue, backgroundGreen, backgroundPink, backgroundYellow;
    public Sprite character_Female, character_Green, character_Red;
    public Sprite weapon_Pickaxe, weapon_Axe, weapon_Bat;
    public Sprite helmet_Bike, helmet_Rugby;
    public Sprite singleCoin, singleGem, singleMoney;
    public Sprite chestSprite;
    public Sprite characterTokenSprite;
    public AudioClip audioClipGold, audioClipGems, audioClipChest;

    // Add more sprites for other reward types
}

[System.Serializable]
public class TrophyRoadMilestone
{
    public int trophyRequirement;
    public TrophyRoadReward reward;
}

[System.Serializable]
public class TrophyRoadReward
{
    public TrophyRewardType rewardType;
    public int amount;
    public string description;
}

public enum TrophyRewardType
{
    Coins_Small, Coins_Medium, Coins_Large,
    Gems_Small, Gems_Medium, Gems_Large,
    Money_Small, Money_Medium, Money_Large,
    Character_Female, Character_Green, Character_Red,
    Weapon_Pickaxe, Weapon_Axe, Weapon_Bat,
    Chest_Currency,
    Helmet_Bike, Helmet_Rugby,
    Character_Token
    // Add more reward types here
}
