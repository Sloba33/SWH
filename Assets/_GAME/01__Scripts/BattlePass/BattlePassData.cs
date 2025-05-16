using UnityEngine;

[CreateAssetMenu(fileName = "BattlePassData", menuName = "ScriptableObjects/BattlePassData", order = 1)]
public class BattlePassData : ScriptableObject
{
    public BattlePassSlot[] slots;
    public Sprite coinSprite_Small, gemSprite_Small, moneySprite_Small;
    public Sprite coinSprite_Medium, gemSprite_Medium, moneySprite_Medium;
    public Sprite coinSprite_Large, gemSprite_Large, moneySprite_Large;
    public Sprite backgroundBlue, backgroundGreen, backgroundPink, backgroundYellow;
    public AudioClip audioClipGold, audioClipGems;
}