using System.Collections.Generic;
using UnityEngine;
using TetraCreations;

public class CharacterTokenManager : MonoBehaviour
{
    public CharacterCollection characterCollection;
    public CharacterStats characterStats;
    public CharacterPickerManager characterPickerManager;
    public CharacterSelector characterSelector;
    public List<CharacterSelector> characterSelectors;
    public CharacterStats previousCharacterStats;

    public int Tokens;
    private void Start()
    {
        LoadTokens();
        characterPickerManager = FindObjectOfType<CharacterPickerManager>(true);
        characterSelectors = characterPickerManager.characterSelectors;
        SetCurrentCharacterToUnlock();
    }
    public void LoadTokens()
    {
        Tokens = PlayerPrefs.GetInt("CharacterTokens", 0);
    }
    [TetraCreations.Attributes.Button("Test Add Tokens")]
    public void TestAddTokens()
    {
        AddTokens(100);
    }
    public bool twice, isEqual;
    public void AddTokens(int tokens)
    {
        twice = false;
        isEqual = false;
        Debug.Log("--------------------------- Handling adding tokens--------------------------------");
        Debug.Log("Tokens : " + Tokens + " Tokens to Add : " + tokens);
        Tokens += tokens;
        Debug.Log("Tokens after adding :" + Tokens);
        Debug.Log("Character stats : " + characterStats.name);
        Debug.Log("Character stats tokens : " + characterStats.tokensRequired);
        PlayerPrefs.SetInt("CharacterTokens", Tokens);
        if (characterStats == null) { Debug.LogError("Character stats not assigned"); return; }
        if (Tokens >= characterStats.tokensRequired)
        {

            isEqual = true;
            Tokens -= characterStats.tokensRequired;
            PlayerPrefs.SetInt("CharacterTokens", Tokens);
            UnlockCharacter();
        }

        // characterPickerManager.UpdateCharacterStats();
        Debug.Log("---------------------------- Finished adding tokens--------------------------------");

    }
    
    public void UpdateTokens()
    {

    }
    public void SetCurrentCharacterToUnlock()
    {
        int index = PlayerPrefs.GetInt("CharacterToUnlock", 0);
        if (index < characterCollection.TokenCharacters.Count)
        {
            characterStats = characterCollection.TokenCharacters[index];
        }
        else Debug.Log("No characters to unlock");
    }
    public void UnlockCharacter()
    {
        Debug.Log("attempting to unlock character");
        for (int i = 1; i < characterSelectors.Count; i++)
        {
            if (characterStats.characterName == characterSelectors[i].characterStats.characterName)
            {
                Debug.Log("Unlocking Character :" + characterStats.characterName);
                characterPickerManager.UnlockCharacter(characterSelectors[i], false);
                SetNextCharacterToUnlock();
                return;
            }
            else Debug.LogError("Character not found!");
        }

    }
    public void SetNextCharacterToUnlock()
    {
        PlayerPrefs.SetInt("CharacterToUnlock", PlayerPrefs.GetInt("CharacterToUnlock", 0) + 1);
        SetCurrentCharacterToUnlock();
    }


}
