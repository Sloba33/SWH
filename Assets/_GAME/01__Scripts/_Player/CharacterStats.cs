using System.ComponentModel;
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
    [ReadOnly(true)] public float maxStrenght;
    public float speed;
    [ReadOnly(true)] public float maxSpeed;
    public float speedMultiplier;
    public float specialPower;
    public float specialMultiplier;
    [ReadOnly(true)] public float maxSpecial;
    public int upgradeCostCoins;
    public int upgradeCostMoney;
    public int upgradeCostMultiplier;
    public int tokensCurrent, tokensRequired, unlockPrice;
    public int Ad_Tokens, Ad_Tokens_Required, Ad_unlock_price;
    
    public float MaxStrength
    {
        get
        {
            // Calculate max strength based on your formula
            string charName = characterName.Replace(" ", "_"); // Sanitize name for PlayerPrefs
            float currentStrength = PlayerPrefs.GetFloat(charName + "_strength", strength);
            return currentStrength + strenghtMultiplier;
        }
    }

    public float MaxSpeed
    {
        get
        {
            // Similar calculation for speed (adjust formula as needed)
            string charName = characterName.Replace(" ", "_");
            float currentSpeed = PlayerPrefs.GetFloat(charName + "_speed", speed);
            return currentSpeed + speedMultiplier;
        }
    }

    public float MaxSpecial
    {
        get
        {
            // Similar calculation for special power (adjust formula as needed)
            string charName = characterName.Replace(" ", "_");
            float currentSpecial = PlayerPrefs.GetFloat(charName + "_special", specialPower);
            return currentSpecial + specialMultiplier;
        }
    }

    // This method will be called when the scriptable object is enabled or values change
    private void OnValidate()
    {
        // Update the serialized fields to show calculated values in inspector
        maxStrenght = MaxStrength;
        maxSpeed = MaxSpeed;
        maxSpecial = MaxSpecial;
    }

    // Method to refresh the calculated values (call this when needed)
    public void RefreshCalculatedValues()
    {
        maxStrenght = MaxStrength;
        maxSpeed = MaxSpeed;
        maxSpecial = MaxSpecial;
    }
}
