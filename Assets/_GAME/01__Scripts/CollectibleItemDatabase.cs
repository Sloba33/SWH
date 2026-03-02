using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Collectible Item DB")]
public class CollectibleItemDatabase : ScriptableObject
{
    public List<GameObject> Match4 = new();
    public List<GameObject> Match5 = new();
    public List<GameObject> Match7 = new();
    public List<GameObject> Match9 = new();
    public string itemId;
    public string displayName;
    public Sprite icon;

    public RewardType rewardType;

    // Used only for color-based rewards
    public bool isColorBased;
    public enum RewardType
{
    HelmetRepair,
    HammerRefill,
    Donut,
    Juice,
    Bomb,
    Rocket,
    ColoredBomb,
    Stopwatch,
    TNT
}
}