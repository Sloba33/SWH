using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPickerManager : MonoBehaviour
{
    private CharacterTokenManager characterTokenManager;
    public GameEvent characterSetterEvent;
    public List<CharacterSelector> characterSelectors = new();
    public CharacterSelector currentCharacter;
    public Transform content;
    public Button upgradeButton, purchaseButton, adRewardButton;
    public TextMeshProUGUI upgradePriceText_coins, upgradePriceText_money, buyPriceText, adRewardText;
    public RectTransform levelImage;
    private CharacterSelector previousCharacter;
    private PlayerMenu currentCharacterPlayerMenu;
    private CharacterManager characterManager;
    private AdsManager adsManager;
    public CustomizationPanelManager customizationPanelManager;
    public bool TrophyRoadTempFlag;
    private void Start()
    {
        adsManager = FindObjectOfType<AdsManager>(true);
        mainMenuManager = FindObjectOfType<MainMenuManager>(true);
        characterManager = FindObjectOfType<CharacterManager>(true);
        customizationPanelManager = FindObjectOfType<CustomizationPanelManager>(true);
        upgradeButton = mainMenuManager.priceTextCoins.transform.parent.GetComponent<Button>();
        characterTokenManager = FindObjectOfType<CharacterTokenManager>(true);
        if (characterSelectors.Count == 0)
            for (int i = 0; i < content.childCount; i++)
            {
                characterSelectors.Add(content.GetChild(i).GetComponent<CharacterSelector>());
                characterSelectors[i].characterPickerManager = this;
            }
        int id = PlayerPrefs.GetInt("SelectedCharacterID", 0);
        Debug.Log("ID: " + id);
        currentCharacter = characterSelectors[id];

        UpdateCharacterStats();
    }
    public PlayerMenu FindCharacterByID()
    {
        int index = PlayerPrefs.GetInt("SelectedCharacterID", 0);
        return characterSelectors[index].playerMenu;
    }
    private void OnEnable()
    {

    }

    public void UpdateCharacterStats()
    {
        Debug.Log("Current character is :" + currentCharacter);
        if (currentCharacter != null)
        {
            Debug.Log("Swapped Character to " + currentCharacter.characterStats.characterName);

            int lvl = PlayerPrefs.GetInt(currentCharacter.characterStats.characterName + "_level", 0);
            float str = PlayerPrefs.GetFloat(currentCharacter.characterStats.characterName + "_strength", currentCharacter.playerMenu.characterStats.strength);
            float speed = PlayerPrefs.GetFloat(currentCharacter.characterStats.characterName + "_speed", currentCharacter.playerMenu.characterStats.speed);
            float special = PlayerPrefs.GetFloat(currentCharacter.characterStats.characterName + "_special", currentCharacter.playerMenu.characterStats.specialPower);
            if (currentCharacter.unlocked)
            {
                Debug.Log(currentCharacter.name + " is unlocked?" + currentCharacter.unlocked);
                purchaseButton.gameObject.SetActive(false);
                adRewardButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(true);
                // Load character stats from PlayerPrefs

                Debug.Log("TrophyRoadTempFlag : " + TrophyRoadTempFlag);
                if (!TrophyRoadTempFlag)
                {
                    Debug.Log("Troph road flag was false, meaning we didnt call  it from  trophy road");
                    StartCoroutine(HandleFillBars(str, speed, special, lvl));
                }
                // if (mainMenuManager != null)
                mainMenuManager.levelText.text = lvl.ToString();
                float priceC = currentCharacter.playerMenu.characterStats.upgradeCostCoins * (lvl + 1);
                float priceM = currentCharacter.playerMenu.characterStats.upgradeCostMoney * (lvl + 1);
                upgradePriceText_coins.text = (priceC).ToString();
                Debug.Log("Upgrade price Text is : " + (priceM).ToString());
                upgradePriceText_money.text = (priceM).ToString();
                // Debug.Log("Main menu manager" + mainMenuManager.name);
                Debug.Log("Current character " + currentCharacter.name);
                // Update color based on affordability
                mainMenuManager.priceTextCoins.color = PlayerPrefs.GetInt("coins") < currentCharacter.playerMenu.characterStats.upgradeCostCoins ? Color.red : Color.white;
                mainMenuManager.priceTextMoney.color = PlayerPrefs.GetInt("money") < currentCharacter.playerMenu.characterStats.upgradeCostMoney ? Color.red : Color.white;

                // Set level UI
                SetLevelUI(lvl);


                // Set character name in UI
                mainMenuManager.characterName.text = currentCharacter.characterStats.characterName;


                // Disable upgrade button if level is maxed
                if (lvl >= 6)
                {
                    Debug.Log("Level was higher than 6");
                    upgradeButton.interactable = false;
                }
                else
                {
                    Debug.Log("Level was lower than 6");
                    upgradeButton.interactable = true;
                }
            }
            else if (!currentCharacter.isAdReward && !currentCharacter.unlocked)
            {
                Debug.Log(currentCharacter.name + "  is unlocked?: " + currentCharacter.unlocked);
                purchaseButton.gameObject.SetActive(true);
                adRewardButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(false);
                buyPriceText.text = GetCharacterPrice(currentCharacter.characterStats).ToString();
                Debug.Log("Buy Price Text is : " + GetCharacterPrice(currentCharacter.characterStats).ToString());
                currentCharacter.characterStats.unlockPrice = GetCharacterPrice(currentCharacter.characterStats);

                mainMenuManager.characterName.text = currentCharacter.characterStats.characterName;
                if (!TrophyRoadTempFlag)
                {
                    Debug.Log("Troph road flag was false, meaning we didnt call  it from  trophy road");
                    StartCoroutine(HandleFillBars(str, speed, special, lvl));
                }
            }
            else
            {
                Debug.Log("Toby is unlocked?: " + currentCharacter.unlocked);
                adRewardButton.gameObject.SetActive(true);
                purchaseButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(false);
                adRewardText.text = GetCharacterAdTokensAmount(currentCharacter.characterStats).ToString() + " / " + currentCharacter.characterStats.Ad_unlock_price;
                currentCharacter.characterStats.Ad_Tokens = GetCharacterAdTokensAmount(currentCharacter.characterStats);
            }
            if (currentCharacter.customizationPanelManager != null)
                if (!currentCharacter.isEditable)
                {
                    currentCharacter.customizationPanelManager.ToggleEditingTabs(false);
                }
                else currentCharacter.customizationPanelManager.ToggleEditingTabs(true);
        }
        else
        {
            Debug.Log("NULLOMEGALUL");
        }
    }

    public void UpgradeStrength(float amount)
    {
        currentCharacter.playerMenu.characterStats.strength += amount;
        SaveCharacterStats();
        UpdateCharacterStats();
    }

    public IEnumerator HandleFillBars(float str, float spd, float spc, int lvl)
    {
        if (lvl == 6)
        {
            str = currentCharacter.playerMenu.characterStats.maxStrenght;
            spd = currentCharacter.playerMenu.characterStats.maxSpeed;
            spc = currentCharacter.playerMenu.characterStats.maxSpecial;
        }

        // Base fill calculations for strength, speed, and special power
        float baseStrengthFill = currentCharacter.playerMenu.characterStats.strength / 20;
        float strengthFillChunk = (1 - baseStrengthFill) / 6;
        float baseSpeedFill = currentCharacter.playerMenu.characterStats.speed / 4;
        float speedFillChunk = (1 - baseSpeedFill) / 6;
        float baseSpecialFill = currentCharacter.playerMenu.characterStats.specialPower / 8;
        float specialFillChunk = (1 - baseSpecialFill) / 6;

        // Target fill amounts
        float targetStrFill = baseStrengthFill + (strengthFillChunk * lvl);
        float targetSpdFill = baseSpeedFill + (speedFillChunk * lvl);
        float targetSpecialFill = baseSpecialFill + (specialFillChunk * lvl);

        // Text values to display
        int startStrText = (int)(currentCharacter.playerMenu.characterStats.strength * 10);
        int targetStrText = (int)(str * 10);

        int startSpdText = (int)(currentCharacter.playerMenu.characterStats.speed * 100);
        int targetSpdText = (int)(spd * 100);

        int startSpecialText = (int)(currentCharacter.playerMenu.characterStats.specialPower * 10);
        int targetSpecialText = (int)(spc * 10);

        // UI starting fill amounts
        float startStrFill = mainMenuManager.strengthFillBar.fillAmount;
        float startSpdFill = mainMenuManager.speedFillBar.fillAmount;
        float startSpecialFill = mainMenuManager.specialFillBar.fillAmount;

        float duration = 0.4f; // Duration in seconds to fill both bars
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Calculate the current fill amount and text based on elapsed time
            float t = elapsedTime / duration;

            // Update fill amounts using Lerp
            mainMenuManager.strengthFillBar.fillAmount = Mathf.Lerp(startStrFill, targetStrFill, t);
            mainMenuManager.speedFillBar.fillAmount = Mathf.Lerp(startSpdFill, targetSpdFill, t);

            // Lerp text values
            int currentStrText = (int)Mathf.Lerp(startStrText, targetStrText, t);
            int currentSpdText = (int)Mathf.Lerp(startSpdText, targetSpdText, t);

            // Update text fields
            mainMenuManager.strengthStatText.text = currentStrText.ToString();
            mainMenuManager.speedStatText.text = currentSpdText.ToString() + "";

            if (lvl % 2 == 0)
            {
                mainMenuManager.specialFillBar.fillAmount = Mathf.Lerp(startSpecialFill, targetSpecialFill, t);
                int currentSpecialText = (int)Mathf.Lerp(startSpecialText, targetSpecialText, t);
                mainMenuManager.specialStatText.text = currentSpecialText.ToString() + "";
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure bars and text reach their target values
        mainMenuManager.strengthFillBar.fillAmount = targetStrFill;
        mainMenuManager.speedFillBar.fillAmount = targetSpdFill;
        mainMenuManager.strengthStatText.text = targetStrText.ToString();
        mainMenuManager.speedStatText.text = targetSpdText.ToString() + "";

        if (lvl % 2 == 0)
        {
            mainMenuManager.specialFillBar.fillAmount = targetSpecialFill;
            mainMenuManager.specialStatText.text = targetSpecialText.ToString() + "";
        }
    }
    public MainMenuManager mainMenuManager;
    private UIShadow uiShadow;
    public void SetCharacter(CharacterSelector characterSelector, bool isFromTokens)
    {

        if (characterSelector.customizationPanelManager != null)
            if (!characterSelector.isEditable)
            {
                characterSelector.customizationPanelManager.ToggleEditingTabs(false);
            }
            else characterSelector.customizationPanelManager.ToggleEditingTabs(true);

        if (TrophyRoadTempFlag)
        {
            Debug.Log("Attempting to Set Character - Called From Trophy Road - Skipping setting character");
            TrophyRoadTempFlag = false;
            return;
        }
        currentCharacter = characterSelector;
        purchaseButton.onClick.RemoveAllListeners();
        adRewardButton.onClick.RemoveAllListeners();
        if (uiShadow != null) uiShadow.enabled = false;

        if (!currentCharacter.unlocked && !currentCharacter.isAdReward)
        {
            purchaseButton.gameObject.SetActive(true);
            upgradeButton.gameObject.SetActive(false);
            adRewardButton.gameObject.SetActive(false);
            purchaseButton.onClick.AddListener(() =>
                              {
                                  UnlockCharacter(characterSelector, true);
                              });
            currentCharacter.uiShadow.effectColor = Color.red;
        }
        else if (currentCharacter.isAdReward && !currentCharacter.unlocked)
        {
            upgradeButton.gameObject.SetActive(false);
            purchaseButton.gameObject.SetActive(false);
            adRewardButton.gameObject.SetActive(true);
            adRewardButton.onClick.AddListener(() =>
                             {
                                 adsManager.ShowRewarded(currentCharacter.characterStats.characterName + "_ad_tokens");
                                 UnlockAdCharacter();
                             });
            currentCharacter.uiShadow.effectColor = Color.green;
        }
        else if (currentCharacter.unlocked)
        {
            upgradeButton.gameObject.SetActive(true);
            purchaseButton.gameObject.SetActive(false);
            adRewardButton.gameObject.SetActive(false);
            for (int i = 0; i < characterSelectors.Count; i++)
            {
                if (characterSelectors[i] == currentCharacter)
                {
                    PlayerPrefs.SetInt("SelectedCharacterID", i);
                    Debug.Log("This is the character :" + characterSelectors[i].name);
                }
            }
            currentCharacter.uiShadow.effectColor = Color.green;
        }
        currentCharacter.uiShadow.enabled = true;
        uiShadow = currentCharacter.uiShadow;
        bool characterNotificationClicked = PlayerPrefs.GetInt(characterSelector.characterStats.characterName.ToString() + "_clicked") != 1;
        if (characterNotificationClicked)
        {
            PlayerPrefs.SetInt(characterSelector.characterStats.characterName + "_clicked", 1);
            Debug.Log("Item clicked? :" + PlayerPrefs.GetInt(characterSelector.characterStats.characterName + "_clicked"));
            characterSelector.notificationImage.SetActive(false);

        }
        Debug.Log("Sending character setter event");
        characterSetterEvent.Raise(this, currentCharacter.playerMenu);
        currentCharacter.playerMenu.SetDefaultColors();

        TrophyRoadTempFlag = false; // reset this after claimign in trouphy road
        UpdateCharacterStats();
    }
    private void UnlockAdCharacter()
    {
        Debug.Log(PlayerPrefs.GetInt(currentCharacter.characterStats.characterName + "_ad_tokens"));
        if (PlayerPrefs.GetInt(currentCharacter.characterStats.characterName + "_ad_tokens") >= currentCharacter.characterStats.Ad_unlock_price)
        {
            UnlockCharacter(currentCharacter, false);
        }
        UpdateCharacterStats();
        mainMenuManager.EnableWorkersNotification(true);

    }
    public int GetCharacterPrice(CharacterStats characterStats)
    {
        int tokensCollected = PlayerPrefs.GetInt("CharacterTokens");
        int remainingTokens = characterStats.tokensRequired - tokensCollected;
        float tokenRatio = (float)remainingTokens / characterStats.tokensRequired;
        return Mathf.CeilToInt(characterStats.unlockPrice * tokenRatio);
    }
    public int GetCharacterAdTokensAmount(CharacterStats characterStats)
    {
        int Ad_TokensCollected = PlayerPrefs.GetInt(characterStats.characterName + "_ad_tokens", 0);
        return Ad_TokensCollected;
    }
    private bool isRewardedAd;
    public void UnlockCharacter(CharacterSelector characterSelector, bool buying)
    {
        Debug.Log("Unlocking character :" + characterSelector + "with character name :" + characterSelector.characterStats.characterName);
        int price = GetCharacterPrice(characterSelector.characterStats);

        if (buying)
        {
            if (PlayerPrefs.GetInt("gems") >= GetCharacterPrice(characterSelector.characterStats))
            {
                PlayerPrefs.SetInt(characterSelector.characterStats.characterName.ToString(), 1);
                purchaseButton.gameObject.SetActive(false);
                currentCharacter.unlocked = true;
                Debug.Log("Setting character " + currentCharacter.characterStats.characterName + " to unlocked");
                currentCharacter.LockCharacter(false);
                PlayerPrefs.SetInt("gems", PlayerPrefs.GetInt("gems") - price);
                SetCharacter(currentCharacter, TrophyRoadTempFlag);
                purchaseButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(true);
                if (!currentCharacter.playerMenu.defaultHelmetItem.unlocked)
                {
                    characterManager.helmetItemManager.UnlockHelmet(currentCharacter.playerMenu.defaultHelmet);
                }
                UpdateCharacterStats();
                mainMenuManager.EnableWorkersNotification(true);
            }
            else
                Debug.Log(" NO MONEY ");
        }
        else
        {
            PlayerPrefs.SetInt(characterSelector.characterStats.characterName.ToString(), 1);
            characterSelector.unlocked = true;
            characterSelector.LockCharacter(false);
            mainMenuManager.EnableWorkersNotification(true);
            // Debug.Log("Setting character " + currentCharacter.characterStats.characterName + " to unlocked");
        }
        if (characterSelector.unlocked)
        {
            Debug.Log("Character selector" + characterSelector.characterStats.characterName + " unlocked? : " + characterSelector.unlocked);
            SetCharacter(characterSelector, TrophyRoadTempFlag);
        }
    }
    public int characterCheckmarkIndex;
    public GameObject checkmarkPrefab;
    public void SetStartingCheckmarks()
    {
        characterCheckmarkIndex = PlayerPrefs.GetInt("SelectedCharacterID", 0);
        Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
        checkmarkPrefab.GetComponent<RectTransform>().SetParent(content.GetChild(characterCheckmarkIndex));
        checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;
    }
    public void SetLevelUI(int level)
    {
        for (int i = 0; i < mainMenuManager.levelBars.Count; i++)
        {
            if (mainMenuManager.levelBars[i] != null)
                mainMenuManager.levelBars[i].SetActive(false);
        }
        Debug.Log("Level : " + level);
        for (int i = 0; i < level; i++)
        {
            if (mainMenuManager.levelBars[i] != null)
                mainMenuManager.levelBars[i].SetActive(true);
            else Debug.Log("level bar not assigned");
            if (mainMenuManager.priceTextCoins != null && mainMenuManager.priceTextMoney != null)
            {
                upgradePriceText_coins.text = "" + currentCharacter.playerMenu.characterStats.upgradeCostCoins * (level + 1);
                upgradePriceText_money.text = "" + currentCharacter.playerMenu.characterStats.upgradeCostMoney * (level + 1);
                upgradePriceText_coins.color = PlayerPrefs.GetInt("coins") < currentCharacter.playerMenu.characterStats.upgradeCostCoins * level ? Color.red : Color.white;
                upgradePriceText_money.color = PlayerPrefs.GetInt("money") < currentCharacter.playerMenu.characterStats.upgradeCostMoney * level ? Color.red : Color.white;
            }
            else Debug.Log("price text not assigned");

        }
    }
    private void SaveCharacterStats()
    {
        string charName = currentCharacter.characterStats.characterName;
        PlayerPrefs.SetInt(charName + "_level", currentCharacter.playerMenu.characterStats.level);
        PlayerPrefs.SetFloat(charName + "_strength", PlayerPrefs.GetFloat(charName + "_strength", currentCharacter.playerMenu.characterStats.strength));
        PlayerPrefs.SetFloat(charName + "_speed", PlayerPrefs.GetFloat(charName + "_speed", currentCharacter.playerMenu.characterStats.speed));
        PlayerPrefs.SetFloat(charName + "_special", PlayerPrefs.GetFloat(charName + "_special", currentCharacter.playerMenu.characterStats.specialPower));
        PlayerPrefs.Save();
    }
    public void RemoveShadows()
    {
        for (int i = 0; i < characterSelectors.Count; i++)
        {
            if (characterSelectors[i].GetComponent<UIShadow>() != null)
                characterSelectors[i].uiShadow.enabled = false;
        }
    }

}
