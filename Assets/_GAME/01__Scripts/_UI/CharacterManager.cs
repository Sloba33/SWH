using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public class CharacterManager : GloballyAccessibleBase<CharacterManager>
{
    public GameObject currentWeapon, currentHelmet;
    public MainMenuManager mainMenuManager;
    public HelmetItem helmetItem;
    public Helmet helmet;
    public PlayerMenu currentCharacter;
    public PlayerController currentGameplayCharacter;
    public List<PlayerMenu> characters = new();
    public Transform weaponsAtBoxParent;
    public List<PlayerController> gameplayCharacters = new();
    public int characterIndex, weaponIndex, helmetIndex;
    public CustomizationPanelManager customizationPanelManager;
    [SerializeField] private AudioClip audioClip;

    // private CharacterPickerManager characterPickerManager;

    private void Start()
    {
        // IronSource.Agent.validateIntegration();
        PlayerPrefs.SetInt("Toby", 1);
        Application.targetFrameRate = 60;
        if (Application.isMobilePlatform)
        {
            int wid = Screen.width;
            int hei = Screen.height;
            QualitySettings.vSyncCount = 0;
            Screen.SetResolution(wid, hei, FullScreenMode.ExclusiveFullScreen, new RefreshRate() { numerator = 60, denominator = 1 });
        }

        for (int i = 0; i < customizationPanelManager.colorButtonManagers.Count; i++)
        {
            customizationPanelManager.colorButtonManagers[i].LoadColorButtons();
        }
        if (characterPickerManager == null) characterPickerManager = FindObjectOfType<CharacterPickerManager>(true);
        SetStartingHelmets();
        LoadCharacter(false);

        StartCoroutine(LoadHelmet(false));

        weaponItemManager.SetReferencesStart();
        LoadWeapon(false);

        currentCharacter.GetComponent<Animator>().SetBool("Picking", false);

        StartCoroutine(TestDefaults(false));

        currentCharacter.SetColors();
        previousWeapon = currentWeapon;
        characterPickerManager.UpdateCharacterStats();

    }
    public void PlayClick()
    {
        AudioManager.Instance.PlayUISound("click");
    }
    private void SetStartingHelmets()
    {
        if (characterPickerManager.characterSelectors.Count == 0) Debug.Log("Count is 0");
        if (characterPickerManager.characterSelectors == null) Debug.Log("No character selector assigned");
        if (characterPickerManager.characterSelectors.Count == 0)
        {
            for (int i = 0; i < characterPickerManager.content.childCount; i++)
            {
                characterPickerManager.characterSelectors.Add(characterPickerManager.content.GetChild(i).GetComponent<CharacterSelector>());
                characterPickerManager.characterSelectors[i].characterPickerManager = characterPickerManager;
            }
        }
        for (int i = 0; i < characterPickerManager.characterSelectors.Count; i++)
        {
            Helmet defHelmet = characterPickerManager.characterSelectors[i].playerMenu.defaultHelmet;
            defaultHelmetItem = FindDefaultHelmetItem(defHelmet);
            characterPickerManager.characterSelectors[i].playerMenu.defaultHelmetItem = defaultHelmetItem;
        }
    }
    private HelmetItem FindDefaultHelmetItem(Helmet helmet)
    {
        for (int i = 0; i < helmetItemManager.helmetItems.Count; i++)
        {
            if (helmetItemManager.helmetItems[i].helmet == helmet)
            {
                return helmetItemManager.helmetItems[i]; // Exit immediately if a match is found
            }
        }

        // If no match is found, return the default item
        return helmetItemManager.helmetItems[0];
    }
    public GameObject boxWeapon;
    private WeaponItem weaponItem;
    public GameObject previousWeapon;
    public GameObject previousHelmet;
    public void SetWeapon(Component sender, object data)
    {
        Debug.Log("Setting weapon");
        weaponItem = (WeaponItem)data;
        // spawn weapon in players hand - MENU
        if (currentWeapon != null)
        {
            Debug.Log("Destroying current weapon");
            // Destroy(previousWeapon);
            Destroy(currentWeapon);
            if (weaponItem.unlocked) Destroy(boxWeapon);
        }
        previousWeapon = weaponItemManager.FindWeaponByID().weaponToSpawn.GetComponent<Weapon>().WeaponStandard;
        currentWeapon = Instantiate(weaponItem.weaponToSpawn.GetComponent<Weapon>().WeaponStandard, currentCharacter.weaponsInHand);

        if (weaponItem.unlocked)
        {
            Debug.Log("Weapon is unlocked");
            // if (previousWeapon != null) Destroy(previousWeapon);
            weaponIndex = PlayerPrefs.GetInt("SelectedWeaponID", 0);
            previousWeapon = weaponItemManager.FindWeaponByID().weaponToSpawn.GetComponent<Weapon>().WeaponStandard;
        }

        currentWeapon.SetActive(true);


        // spawn weapon in players hand - GAMEPLAY
        if (weaponItem.unlocked)
        {
            Debug.Log("Setting weapon at box");
            boxWeapon = Instantiate(weaponItem.weaponAtBox, weaponsAtBoxParent);
            PlayerAttack playerAttack = currentGameplayCharacter.GetComponent<PlayerAttack>();
            playerAttack.weapon = weaponItem.weaponToSpawn;
        }
        if(!currentCharacter.weaponsInHand.gameObject.activeSelf) currentCharacter.weaponsInHand.gameObject.SetActive(true);

    }
    private bool characterChanged;
    public CharacterSelector currentCharacterSelector;
    public void RevertSelectionStates()
    {

        ClearShadows();

        if (!characterChanged)
        {
            Destroy(currentWeapon);
            Destroy(currentHelmet);
            // }
            if (previousWeapon != null)
            {
                currentWeapon = Instantiate(previousWeapon, currentCharacter.weaponsInHand);
                currentWeapon.SetActive(true);
                // Destroy(previousWeapon);
                previousWeapon = null;
            }
            if (previousHelmet != null && !helmetItem.unlocked)
            {
                Debug.Log("Helmet not unlocked");
                currentHelmet = Instantiate(previousHelmet, currentCharacter.helmetParent);
                currentHelmet.SetActive(true);
                SetDefaultHelmet(currentCharacter);

                // Destroy(previousHelmet);
                previousHelmet = null;
                // StartCoroutine(LoadHelmet(true));

            }
        }
        if (currentCharacter != null)
        {
            if (previousCharacter != null)
            {
                Debug.Log("Character Was Changed");
                currentCharacter.gameObject.SetActive(false);
                currentCharacter = previousCharacter;
                previousCharacter = null;
            }

            currentCharacter.gameObject.SetActive(true);
            if (currentCharacterSelector != null && currentCharacterSelector.unlocked)
                StartCoroutine(LoadHelmet(false));
            else StartCoroutine(LoadHelmet(true));
            // SetDefaultHelmet();
        }
        if (currentCharacterSelector != null && !currentCharacterSelector.unlocked)
        {
            SetDefaultHelmet(currentCharacter);
        }
        characterChanged = false;

    }
    public void SetDefaultHelmet(PlayerMenu character)
    {
        Debug.Log("Setting Default Helmet Colors");
        Debug.Log("Interacting with : " + character);
        character.ColorHat(helmet.defaultColor);
        helmet = helmetItem.helmet;
        Debug.Log("Setting default helmet to : " + helmetItem.name);
        helmet.material.color = helmet.defaultColor;
        helmet.SetTex(character.helmetMaterial);
    }
    private void ClearShadows()
    {
        characterPickerManager.RemoveShadows();
        helmetItemManager.RemoveShadows();
        weaponItemManager.RemoveShadows();
    }
    public void LoadWeapon(bool reset)
    {
        Debug.Log("Loading weapon");
        WeaponItem weaponItem;

        if (reset)
        {
            weaponItem = weaponItemManager.weaponItems[0];
            Debug.Log("Weapon item : " + weaponItem.name);
        }
        else
        {

            weaponItem = weaponItemManager.FindWeaponByID();
            Debug.Log("Weapon item : " + weaponItem.name);
        }

        PlayerAttack playerAttack = currentGameplayCharacter.GetComponent<PlayerAttack>();
        playerAttack.weapon = weaponItem.weaponToSpawn;

        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
            Destroy(boxWeapon);
        }
        currentWeapon = Instantiate(weaponItem.weaponToSpawn.GetComponent<Weapon>().WeaponStandard, currentCharacter.GetComponent<PlayerMenu>().weaponsInHand);
        currentWeapon.SetActive(true);
        // boxWeapon = Instantiate(weaponItem.weaponAtBox, weaponsAtBoxParent);
        // uncomment above line to make weapons on box spawn
    }
    public void SetHelmet(Component sender, object data)
    {
        HelmetItem helmetItem = (HelmetItem)data;
        if (currentHelmet != null)
        {
            Destroy(currentHelmet);
            // Destroy(previousHelmet);
        }
        previousHelmet = helmetItemManager.FindHelmetByID().helmet.gameObject;
        currentHelmet = Instantiate(helmetItem.helmet.gameObject, currentCharacter.helmetParent);

        if (helmetItem.unlocked)
        {
            // Destroy(previousHelmet);
            helmetIndex = PlayerPrefs.GetInt("SelectedHelmetID", 0);
            previousHelmet = helmetItemManager.FindHelmetByID().helmet.gameObject;
            Player player = currentGameplayCharacter.GetComponent<Player>();
            player.helmetToSpawn = helmet;
            PlayerPrefs.SetInt("SelectedHelmetColor", 0);
        }
        currentCharacter.defaultHat = helmet.defaultColor;
        helmet = helmetItem.helmet;
        helmet.material.color = helmet.defaultColor;
        helmet.SetTex(currentCharacter.helmetMaterial);

        currentHelmet.SetActive(true);

        for (int j = 0; j < customizationPanelManager.colorButtonManagers.Count; j++)
        {

            if (customizationPanelManager.colorButtonManagers[j].clothesType == ColorButtonManager.ClothesType.Hat)
            {
                customizationPanelManager.colorButtonManagers[j].colorButtons[0].color = helmet.defaultColor;
                customizationPanelManager.colorButtonManagers[j].SetSpecificCheckmark();
            }
        }
    }
    public IEnumerator LoadHelmet(bool reset)
    {
        yield return null;
        Debug.Log("Loading Helmet");
        if (reset)
        {
            helmetItem = currentCharacter.defaultHelmetItem;
            Debug.Log("Loading default helmet :" + currentCharacter.defaultHelmetItem);
            PlayerPrefs.SetInt("SelectedHelmetID", helmetItem.id);
            helmetItemManager.SetStartingCheckmarks();

        }
        else
        {
            Debug.Log("Grabbing index from prefs");
            helmetItem = helmetItemManager.FindHelmetByID();
            helmetIndex = PlayerPrefs.GetInt("SelectedHelmetID", 0);
        }
        if (currentHelmet != null)
        {
            Destroy(currentHelmet);
        }
        currentHelmet = Instantiate(helmetItem.helmet.gameObject, currentCharacter.helmetParent);

        currentHelmet.SetActive(true);
        helmet = helmetItem.helmet;
        Player player = currentGameplayCharacter.GetComponent<Player>();
        player.helmetToSpawn = helmet;


        // currentCharacter.defaultHat = helmet.defaultColor;

        if (reset)
            helmet.material.color = helmet.defaultColor;

        helmet.SetTex(currentCharacter.helmetMaterial);
        for (int j = 0; j < customizationPanelManager.colorButtonManagers.Count; j++)
        {

            if (customizationPanelManager.colorButtonManagers[j].clothesType == ColorButtonManager.ClothesType.Hat)
            {
                customizationPanelManager.colorButtonManagers[j].colorButtons[0].color = helmet.defaultColor;
                customizationPanelManager.colorButtonManagers[j].SetSpecificCheckmark();
            }
        }
    }
    private HelmetItem defaultHelmetItem;
    public PlayerMenu previousCharacter;
    private UIShadow uishadow;
    public void SetCharacter(Component sender, object data)
    {

        if (characterPickerManager.TrophyRoadTempFlag) { Debug.Log("Called From Trophy Road - Skipping setting character"); return; }
        currentCharacter.gameObject.SetActive(false);
        // PlayerColorsGlobal.Instance.SetGameplayPrefab(characterIndex);
        Debug.Log("Component " + sender.GetType() + "Object :" + data.GetType());
        previousCharacter = characterPickerManager.FindCharacterByID();
        Debug.Log("Previous Character :" + previousCharacter.name);
        currentCharacter = (PlayerMenu)data;
        if (characterPickerManager.currentCharacter.unlocked)
        {
            Debug.Log("Current character Name : " + currentCharacter.name);
            Debug.Log("Previous Character :" + previousCharacter.name);
            characterIndex = PlayerPrefs.GetInt("SelectedCharacterID", 0);
            previousCharacter = currentCharacter;
            currentGameplayCharacter = gameplayCharacters[characterIndex];
            customizationPanelManager.helmetItemManager.currentCharacter = currentCharacter;
            StartCoroutine(LoadHelmet(true));
            LoadWeapon(false);
            mainMenuManager.currentCharacter = currentCharacter;
        }

        // StartCoroutine(SetHelmetReferences(currentCharacter.helmets));
        SetCheckmarkDefaults(true);
        currentCharacter.canBlink = true;
        currentCharacter.hasBlinked = false;


        // Debug.Log("Current character : " + currentCharacter + " has helmets : " + currentCharacter.helmets.Count + " helmetitem manager");
        // profileManager.SetProfileCharacter(characterIndex);

        Debug.Log("Current character : " + currentCharacter + " has picking set to true");
        currentCharacter.gameObject.SetActive(true);
        currentCharacter.GetComponent<Animator>().SetBool("Picking", true);

        customizationPanelManager.UpdateDefaultColors();
        StartCoroutine(LoadHelmet(true));
        characterChanged = true;
    }
    public void LoadCharacter(bool reset)
    {
        // loading character from prefs
        characterIndex = PlayerPrefs.GetInt("SelectedCharacterID", 0);
        Debug.Log("Index : " + characterIndex + " characters count :" + characters.Count);
        currentCharacter = characters[characterIndex];
        currentGameplayCharacter = gameplayCharacters[characterIndex];
        mainMenuManager.currentCharacter = currentCharacter;
        customizationPanelManager.helmetItemManager.currentCharacter = currentCharacter;
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] == currentCharacter)
            {
                characters[i].gameObject.SetActive(true);
            }
            else
                characters[i].gameObject.SetActive(false);
        }
        // StartCoroutine(LoadHelmet(false));

        customizationPanelManager.UpdateDefaultColors();


    }
    public CharacterPickerManager characterPickerManager;
    public HelmetItemManager helmetItemManager;
    public WeaponItemManager weaponItemManager;
    public void SetCheckmarkDefaults(bool reset)
    {
        if (reset)
        {
            PlayerPrefs.SetInt("SelectedHelmetID", 0);
            PlayerPrefs.SetInt("SelectedShirtColor", 0);
            PlayerPrefs.SetInt("SelectedOverallsColor", 0);
            PlayerPrefs.SetInt("SelectedHelmetColor", 0);
            PlayerPrefs.SetInt("SelectedShoesColor", 0);
        }
        for (int i = 0; i < customizationPanelManager.colorButtonManagers.Count; i++)
        {
            customizationPanelManager.colorButtonManagers[i].SetStartingCheckmarks();
        }

        characterPickerManager.SetStartingCheckmarks();
        helmetItemManager.SetStartingCheckmarks();
    }
    public IEnumerator TestDefaults(bool reset)
    {
        yield return new WaitForSeconds(0.1f);
        SetCheckmarkDefaults(reset);
    }

    [SerializeField] ParticleSystem levelUpParticle;
    public void UpgradeStats()
    {
        string charName = currentCharacter.characterStats.characterName;

        // Calculate current upgrade costs based on the character's level
        int currentLevel = PlayerPrefs.GetInt(charName + "_level", 0);
        int upgradeCostCoins = currentCharacter.characterStats.upgradeCostCoins * (currentLevel + 1);
        int upgradeCostMoney = currentCharacter.characterStats.upgradeCostMoney * (currentLevel + 1);

        // Check if the player has enough coins and money for the upgrade
        if (PlayerPrefs.GetInt("coins") >= upgradeCostCoins && PlayerPrefs.GetInt("money") >= upgradeCostMoney)
        {
            // Increase character level and stats
            currentLevel++;
            // float newStrength = PlayerPrefs.GetFloat(charName + "_strength", currentCharacter.characterStats.strength)
            //                     + ((currentCharacter.characterStats.strength / 4) * currentCharacter.characterStats.strenghtMultiplier);
            float newStrength = PlayerPrefs.GetFloat(charName + "_strength", currentCharacter.characterStats.strength)
           + currentCharacter.characterStats.strenghtMultiplier;

            float newSpeed = PlayerPrefs.GetFloat(charName + "_speed", currentCharacter.characterStats.speed) +
                              currentCharacter.characterStats.speedMultiplier;
            float newSpecial = PlayerPrefs.GetFloat(charName + "_special", currentCharacter.characterStats.specialPower) +
                                currentCharacter.characterStats.specialMultiplier;

            // Save updated stats and level
            PlayerPrefs.SetFloat(charName + "_strength", newStrength);
            PlayerPrefs.SetFloat(charName + "_speed", newSpeed);
            PlayerPrefs.SetFloat(charName + "_special", newSpecial);
            PlayerPrefs.SetInt(charName + "_level", currentLevel);

            // Deduct the upgrade costs
            PlayerPrefs.SetInt("coins", PlayerPrefs.GetInt("coins") - upgradeCostCoins);
            PlayerPrefs.SetInt("money", PlayerPrefs.GetInt("money") - upgradeCostMoney);
            mainMenuManager.coinText.text = PlayerPrefs.GetInt("coins").ToString();
            mainMenuManager.moneyText.text = PlayerPrefs.GetInt("money").ToString();

            // Update the UI with the new stats and costs
            characterPickerManager.UpdateCharacterStats();
            Debug.Log("Updated Character Stats");
            if (currentLevel >= 6)
            {
                mainMenuManager.priceTextCoins.transform.parent.GetComponent<Button>().interactable = false;
            }
            GameObject audioObject = Instantiate(new GameObject());
            audioObject.AddComponent<AudioSource>();
            audioObject.GetComponent<AudioSource>().clip = audioClip;
            audioObject.GetComponent<AudioSource>().Play();
            Destroy(audioObject, audioObject.GetComponent<AudioSource>().clip.length);
            // AudioManager.Instance.PlaySound(2);
            // AudioManager.Instance.PlaySound(2);
            // characterPickerManager.levelImage.KillAllTweens();
            if (!DOTween.IsTweening(characterPickerManager.levelImage))
            {
                characterPickerManager.levelImage.DOPunchScale(new Vector3(0.6f, 0.6f, 0.6f), 0.75f, 1).Play();
            }
            if (!levelUpParticle.gameObject.activeSelf)
            {
                levelUpParticle.gameObject.SetActive(true);
                levelUpParticle.Play();
            }
            else levelUpParticle.Play();
        }
    }



    private void LoadCharacterStats(CharacterStats character)
    {
        string charName = character.characterName;
        character.level = PlayerPrefs.GetInt(charName + "_level", character.level);
        character.strength = PlayerPrefs.GetFloat(charName + "_strength", character.strength);
        character.speed = PlayerPrefs.GetFloat(charName + "_speed", character.speed);
        character.specialPower = PlayerPrefs.GetFloat(charName + "_special", character.specialPower);
    }
}
