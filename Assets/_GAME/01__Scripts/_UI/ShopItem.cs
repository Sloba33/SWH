using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum ItemType { Character, Helmet, Weapon, Color }
[System.Serializable]
public class ShopItem
{
    public string itemName;
    public ItemType itemType;
    public int baseGemPrice; // Base price in gems
    public int tokenRequirement; // Token requirement for character unlocks
    public Sprite itemIcon;
    public bool isUnlocked; // True when purchased
}
