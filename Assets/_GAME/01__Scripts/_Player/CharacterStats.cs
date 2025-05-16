using UnityEngine;
[CreateAssetMenu(fileName = "Characters", menuName = "Characters/CharacterStats", order = 1)]
public class CharacterStats : ScriptableObject
{
    public bool female;
    public Sprite characterPortrait;
    public string characterName;
    public int level;
    public float strength;
    public float strenghtMultiplier;
    public float maxStrenght;
    public float speed;
    public float maxSpeed;
    public float speedMultiplier;
    public float specialPower;
    public float specialMultiplier;
    public float maxSpecial;
    public int upgradeCostCoins;
    public int upgradeCostMoney;
    public int upgradeCostMultiplier;
    public int tokensCurrent, tokensRequired, unlockPrice;
    public int Ad_Tokens, Ad_Tokens_Required, Ad_unlock_price;
}
