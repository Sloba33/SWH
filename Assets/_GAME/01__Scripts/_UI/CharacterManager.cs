using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.TextCore.Text;
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
    // private CharacterPickerManager characterPickerManager;
    [SerializeField] private AudioClip audioClip;
    private bool _coinsGranted;
    public bool ColorChanged;
    public CharacterSelector characterSelector;
    public GameObject confirmedHelmetPrefab;
    private bool previewingLockedCharacter;
    public GameObject previewHelmet;
    public TrophyRoadManager trophyRoadManager;
    // private CharacterPickerManager characterPickerManager;
    private void GrantStarterCoins()
    {
        _coinsGranted = PlayerPrefs.GetInt("CoinsGranted", 0) != 0;
        if (!_coinsGranted)
        {
            PlayerPrefs.SetInt("CoinsGranted", 1);
            PlayerPrefs.SetInt("coins", PlayerPrefs.GetInt("coins") + 100);
        }
        PlayerPrefs.Save();
        Debug.Log("Coins Granted: " + _coinsGranted);
    }
    public void CheckIfUpgradesAreAffordable()
    {
        bool anyAffordableUpgrades = false;
        for (int i = 0; i < characters.Count; i++)
        {

            string characterName = characters[i].characterStats.characterName;
            bool unlocked = PlayerPrefs.GetInt(characters[i].characterStats.characterName) == 1;
            if (PlayerPrefs.GetInt(characterName + "_level", 0) >= 6)
            {
                Debug.Log("------ Character " + characters[i].characterStats.characterName + " is at max level, skipping upgrade check ------");
                continue; // Skip characters that are already at max level
            }
            if (unlocked)
            {
                Debug.Log("------ Checking if we have available upgrades for character: " + characters[i].characterStats.characterName + " ------");
                int currentLevel = PlayerPrefs.GetInt(characterName + "_level", 0);
                int upgradeCostCoins = characters[i].characterStats.upgradeCostCoins * (currentLevel + 1);
                int upgradeCostMoney = characters[i].characterStats.upgradeCostMoney * (currentLevel + 1);
                Debug.Log("------ Current Level: " + currentLevel + " | Upgrade Cost Coins: " + upgradeCostCoins + " | Upgrade Cost Money: " + upgradeCostMoney);
                Debug.Log("------Player Coins: " + PlayerPrefs.GetInt("coins", 0) + " | Player Money: " + PlayerPrefs.GetInt("money", 0));
                if (PlayerPrefs.GetInt("coins", 0) >= upgradeCostCoins && PlayerPrefs.GetInt("money", 0) >= upgradeCostMoney)
                {
                    Debug.Log("------We got money");
                    if (mainMenuManager != null)
                    {
                        anyAffordableUpgrades = true;
                        mainMenuManager.upgradeAvailableNotification.gameObject.SetActive(true);

                        Debug.Log("------We have enough currency to upgrade, enabling icon");
                    }
                    else Debug.Log("------MainMenuManager is null, cannot enable upgrade notification");
                }
            }
            if (!anyAffordableUpgrades)
            {
                if (mainMenuManager != null)
                {

                    mainMenuManager.upgradeAvailableNotification.gameObject.SetActive(false);
                    Debug.Log("------We don't have enough currency to upgrade, disabling icon");
                }
                else Debug.Log("------MainMenuManager is null, cannot disable upgrade notification");
            }
        }
    }
    private void Start()
    {
        MainMenuManager mainMenuManager = FindObjectOfType<MainMenuManager>();
        // IronSource.Agent.validateIntegration();

        GrantStarterCoins();
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
        if (characterPickerManager == null) characterPickerManager = FindAnyObjectByType<CharacterPickerManager>(FindObjectsInactive.Include);
        currentCharacterSelector = characterPickerManager.characterSelectorCurrent;
        SetStartingHelmets();
        LoadCharacter(false);

        StartCoroutine(LoadHelmet(false));
        StartCoroutine(SetInitialConfirmedHelmet());
        weaponItemManager.SetReferencesStart();
        // weaponItemManager.SelectWeapon(weaponItemManager.weaponItem);
        Debug.Log("Set weapon to : " + weaponItemManager.weaponItem.name);
        LoadWeapon(false);

        currentCharacter.GetComponent<Animator>().SetBool("Picking", false);

        StartCoroutine(TestDefaults(false));

        currentCharacter.SetColors();
        previousWeapon = currentWeapon;
        characterPickerManager.UpdateCharacterStats();
        CheckIfUpgradesAreAffordable();
        // if (PlayerPrefs.GetInt("gems") < 2000) PlayerPrefs.SetInt("gems", 5000);
        // if (PlayerPrefs.GetInt("coins") < 2000) PlayerPrefs.SetInt("coins", 5000);
        // if (PlayerPrefs.GetInt("money") < 2000) PlayerPrefs.SetInt("money", 5000);
        // PlayerPrefs.SetInt("coins", 300);
        // PlayerPrefs.SetInt("money", 300);


    }
    private IEnumerator SetInitialConfirmedHelmet()
    {
        yield return null; // Ensure helmet has been instantiated

        if (currentHelmet != null && helmet != null)
        {
            confirmedHelmetPrefab = helmet.gameObject;
            Debug.Log("[INIT] Setting confirmedHelmetPrefab to: " + confirmedHelmetPrefab.name);
        }
    }
    void OnEnable()
    {
        currentCharacterSelector = characterPickerManager.characterSelectorCurrent;
        Debug.Log("[THIS] - CharacterManager enabled, currentCharacterSelector: " + currentCharacterSelector);
    }
    public void PlayClick()
    {
        AudioManager.Instance.PlayUISound("click");
    }
    private void SetStartingHelmets()
    {
        if (characterPickerManager.characterSelectors.Count == 0) Debug.Log("[SetStartingHelmet] - Count is 0");
        if (characterPickerManager.characterSelectors == null) Debug.Log("SetStartingHelmet]  No character selector assigned");
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
    public WeaponItem weaponItem;
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
        if (!currentCharacter.weaponsInHand.gameObject.activeSelf) currentCharacter.weaponsInHand.gameObject.SetActive(true);

    }
    private bool characterChanged;
    public CharacterSelector currentCharacterSelector;
    public Helmet revertedHelmet;
    public bool wasPreviewingHelmet = false;

    public void RevertSelectionStates()
    {
        if (previewHelmet != null)
        {
            Destroy(previewHelmet);
            previewHelmet = null;
        }
        Debug.Log("[REVERT] RevertSelectionStates CALLED");
        ClearShadows();
        Debug.Log("[REVERT] previousHelmet: " + previousHelmet + ", wasPreviewingHelmet: " + wasPreviewingHelmet);
        // Always revert helmet if needed, regardless of characterChanged
        if (confirmedHelmetPrefab != null && wasPreviewingHelmet)
        {
            Debug.Log("[REVERT] Helmet is locked, reverting to previousHelmet prefab");

            if (currentHelmet != null)
            {
                Debug.Log("[HELMET] - destroying current helmet : " + currentHelmet);

                Destroy(currentHelmet);
            }

            currentHelmet = Instantiate(confirmedHelmetPrefab, currentCharacter.helmetParent);
            Debug.Log("[HELMET] - spawning  helmet : " + currentHelmet);
            currentHelmet.SetActive(true);

            revertedHelmet = currentHelmet.GetComponent<Helmet>();
            if (revertedHelmet != null && !ColorChanged)
            {
                // helmet = revertedHelmet; 
                Debug.Log("[REVERT] Helmet Name: " + revertedHelmet.helmetName);
                Debug.Log("[REVERT] DefaultColor: " + revertedHelmet.defaultColor);
                revertedHelmet.material.color = helmet.defaultColor;
                revertedHelmet.SetTex(currentCharacter.helmetMaterial);
                // revertedHelmet.mesh.material = new Material(revertedHelmet.mesh.material); // ensure instancing
                // revertedHelmet.material = revertedHelmet.mesh.material;
                // revertedHelmet.material.color = revertedHelmet.defaultColor;
                // Material newMat = new Material(revertedHelmet.mesh.material);
                // revertedHelmet.mesh.material = newMat;
                // revertedHelmet.material = newMat;

                // revertedHelmet.material.color = revertedHelmet.defaultColor;
                // revertedHelmet.SetTex(currentCharacter.helmetMaterial);

                for (int j = 0; j < customizationPanelManager.colorButtonManagers.Count; j++)
                {
                    if (customizationPanelManager.colorButtonManagers[j].clothesType == ColorButtonManager.ClothesType.Hat)
                    {
                        customizationPanelManager.colorButtonManagers[j].colorButtons[0].color = revertedHelmet.defaultColor;
                        customizationPanelManager.colorButtonManagers[j].SetSpecificCheckmark();
                    }
                }

                currentCharacter.ColorHat(revertedHelmet.defaultColor);
            }

            previousHelmet = null;
        }

        // Only revert character if changed
        if (characterChanged)
        {
            if (currentCharacter != null && previousCharacter != null)
            {
                Debug.Log("[REVERT] Reverting character");
                currentCharacter.gameObject.SetActive(false);
                currentCharacter = previousCharacter;
                previousCharacter = null;
                currentCharacter.gameObject.SetActive(true);
            }

            if (!previewingLockedCharacter)
            {
                // Normal revert (unlocked character flow)
                StartCoroutine(LoadHelmet(false));
            }
            else
            {
                // Locked character preview → restore last confirmed helmet (if any)

                if (currentHelmet != null)
                    Destroy(currentHelmet);

                if (confirmedHelmetPrefab != null)
                {
                    currentHelmet = Instantiate(confirmedHelmetPrefab, currentCharacter.helmetParent);
                    currentHelmet.SetActive(true);

                    helmet = currentHelmet.GetComponent<Helmet>();
                }
                else
                {
                    // Safety fallback: no confirmed helmet yet → use character default
                    StartCoroutine(LoadHelmet(true));
                }
            }
            previewingLockedCharacter = false;
            characterChanged = false;

            // ✅ Update external references now that currentCharacter has been reverted
            characterPickerManager.characterSelectorCurrent = characterPickerManager.FindCharacterSelectorByCharacter(currentCharacter);
            currentCharacterSelector = characterPickerManager.characterSelectorCurrent;
            customizationPanelManager.currentCharacter = currentCharacter;
            customizationPanelManager.helmetItemManager.currentCharacter = currentCharacter;
            mainMenuManager.currentCharacter = currentCharacter;

        }


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
        WeaponItem wepItem;

        if (reset)
        {
            wepItem = weaponItemManager.weaponItems[0];
            Debug.Log("Weapon item : " + wepItem.name);
        }
        else
        {

            wepItem = weaponItemManager.FindWeaponByID();
            Debug.Log("Weapon item : " + wepItem.name);
        }

        PlayerAttack playerAttack = currentGameplayCharacter.GetComponent<PlayerAttack>();
        playerAttack.weapon = wepItem.weaponToSpawn;

        if (currentWeapon != null)
        {
            Debug.Log("Destroying box weapon");
            Destroy(currentWeapon);
            Destroy(boxWeapon);
        }
        currentWeapon = Instantiate(wepItem.weaponToSpawn.GetComponent<Weapon>().WeaponStandard, currentCharacter.GetComponent<PlayerMenu>().weaponsInHand);
        Debug.Log("WeaponName-- :" + currentWeapon);
        currentWeapon.SetActive(true);
        weaponItem = wepItem;
        EnableBoxWeapon();
        // uncomment above line to make weapons on box spawn
    }
    public void CancelHelmetPreview()
    {
        Debug.Log("[Helmet] Cancel preview");

        if (!wasPreviewingHelmet)
            return;

        // Destroy ONLY the preview
        if (previewHelmet != null)
        {
            Destroy(previewHelmet);
            previewHelmet = null;
        }

        // Re-enable equipped helmet
        if (currentHelmet != null)
        {
            currentHelmet.SetActive(true);

            helmet = currentHelmet.GetComponent<Helmet>();
            helmet.SetTex(currentCharacter.helmetMaterial);
        }

        wasPreviewingHelmet = false;
        previewingLockedCharacter = false;
    }
    public void EnableBoxWeapon()
    {
        if (PlayerPrefs.GetInt("AnyWeaponUnlocked") == 1)
        {

            if (weaponItem != null)
                boxWeapon = Instantiate(weaponItem.weaponAtBox, weaponsAtBoxParent);
            else Debug.LogWarning("weapon item is NULL");
        }
    }
    public void PreviewHelmet(HelmetItem helmetItem, bool isLockedCharacterPreview = false)
    {
        Debug.Log("Previewing Helmet");

        wasPreviewingHelmet = true;

        // ─────────────────────────────────────────
        // LOCKED CHARACTER PREVIEW (already correct)
        // ─────────────────────────────────────────
        if (isLockedCharacterPreview)
        {
            Debug.Log("Character is locked Preview");

            if (previewHelmet != null)
                Destroy(previewHelmet);

            previewHelmet = Instantiate(helmetItem.helmet.gameObject, currentCharacter.helmetParent);
            previewHelmet.SetActive(true);

            Helmet instanceHelmet = previewHelmet.GetComponent<Helmet>();
            if (instanceHelmet != null)
            {
                Material newMat = new Material(instanceHelmet.mesh.material);
                instanceHelmet.mesh.material = newMat;
                instanceHelmet.material = newMat;

                newMat.color = helmetItem.helmet.defaultColor;
                instanceHelmet.SetTex(newMat);
            }

            return;
        }

        // ─────────────────────────────────────────
        // NORMAL HELMET PREVIEW (THIS IS THE FIX)
        // ─────────────────────────────────────────

        if (confirmedHelmetPrefab == null && currentHelmet != null)
            confirmedHelmetPrefab = currentHelmet;

        // 🔑 Hide equipped helmet instead of destroying it
        if (currentHelmet != null)
            currentHelmet.SetActive(false);

        // 🔑 Destroy previous preview if any
        if (previewHelmet != null)
            Destroy(previewHelmet);

        // 🔑 Spawn preview helmet
        previewHelmet = Instantiate(helmetItem.helmet.gameObject, currentCharacter.helmetParent);
        previewHelmet.SetActive(true);

        Helmet normalHelmet = previewHelmet.GetComponent<Helmet>();
        if (normalHelmet != null)
        {
            Material newMat = new Material(normalHelmet.mesh.material);
            normalHelmet.mesh.material = newMat;
            normalHelmet.material = newMat;

            newMat.color = helmetItem.helmet.defaultColor;
            normalHelmet.SetTex(newMat);
        }
    }


    public void PreviewCharacter(PlayerMenu character)
    {
        Debug.Log("Previewing character (not unlocked)");

        if (currentCharacter != null)
            currentCharacter.gameObject.SetActive(false);

        currentCharacter = character;
        currentCharacter.gameObject.SetActive(true);
        currentCharacter.GetComponent<Animator>().SetBool("Picking", true);
        customizationPanelManager.helmetItemManager.currentCharacter = currentCharacter;
        mainMenuManager.currentCharacter = currentCharacter;

        // Preview default helmet
        HelmetItem previewHelmetItem = character.defaultHelmetItem;
        PreviewHelmet(previewHelmetItem, true);

        characterChanged = true;
    }
    public void SetHelmet(Component sender, object data)
    {
        if (previewHelmet != null)
        {
            Destroy(previewHelmet);
            previewHelmet = null;
        }

        HelmetItem helmetItem = (HelmetItem)data;

        bool characterUnlocked = characterPickerManager.characterSelectorCurrent.unlocked;

        if (!helmetItem.unlocked || !characterUnlocked)
        {
            Debug.Log("Helmet or character is locked, previewing helmet.");
            PreviewHelmet(helmetItem);
            return;
        }

        Debug.Log("Setting helmet.");

        if (currentHelmet != null)
        {
            if (helmet.gameObject != null)
                previousHelmet = helmet.gameObject;
            if (currentHelmet != null)
                Destroy(currentHelmet);
        }

        previousHelmet = helmetItemManager.FindHelmetByID().helmet.gameObject;
        currentHelmet = Instantiate(helmetItem.helmet.gameObject, currentCharacter.helmetParent);
        currentHelmet.SetActive(true);

        helmet = helmetItem.helmet;
        helmet.material.color = helmet.defaultColor;
        helmet.SetTex(currentCharacter.helmetMaterial);

        // confirmedHelmetPrefab = helmetItem.helmet.gameObject;
        confirmedHelmetPrefab = currentHelmet;
        wasPreviewingHelmet = false;
        helmetIndex = helmetItem.id;
        PlayerPrefs.SetInt("SelectedHelmetID", helmetIndex);
        PlayerPrefs.SetInt("SelectedHelmetColor", 0);

        Player player = currentGameplayCharacter.GetComponent<Player>();
        player.helmetToSpawn = helmet;

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
        confirmedHelmetPrefab = currentHelmet;
        wasPreviewingHelmet = false;
        ColorChanged = false;
        Player player = currentGameplayCharacter.GetComponent<Player>();
        player.helmetToSpawn = helmet;


        // currentCharacter.defaultHat = helmet.defaultColor;

        if (reset)
        {
            Debug.Log("[LOAD HELMET] -Resetting helmet to default color");
            helmet.material.color = helmet.defaultColor;
        }

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
        if (characterPickerManager.TrophyRoadTempFlag)
        {
            Debug.Log("Called From Trophy Road - Skipping setting character");
            return;
        }

        previousCharacter = characterPickerManager.FindCharacterByID();

        PlayerMenu selectedCharacter = (PlayerMenu)data;

        bool isUnlocked = characterPickerManager.currentCharacter.unlocked;

        if (!isUnlocked)
        {
            previewingLockedCharacter = true;
            PreviewCharacter(selectedCharacter);
            return;
        }

        Debug.Log("Setting character: " + selectedCharacter.name);

        if (currentCharacter != null)
            currentCharacter.gameObject.SetActive(false);

        currentCharacter = selectedCharacter;
        characterIndex = PlayerPrefs.GetInt("SelectedCharacterID", 0);
        currentGameplayCharacter = gameplayCharacters[characterIndex];

        customizationPanelManager.helmetItemManager.currentCharacter = currentCharacter;
        mainMenuManager.currentCharacter = currentCharacter;

        StartCoroutine(LoadHelmet(true));
        LoadWeapon(false);

        SetCheckmarkDefaults(true);
        customizationPanelManager.UpdateDefaultColors();

        currentCharacter.canBlink = true;
        currentCharacter.hasBlinked = false;

        currentCharacter.gameObject.SetActive(true);
        currentCharacter.GetComponent<Animator>().SetBool("Picking", true);
        previewingLockedCharacter = false;
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
            ;
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
        CheckIfUpgradesAreAffordable();
        CheckIfCharactersAreUpgradeable();
    }

    public void CheckIfCharactersAreUpgradeable()
    {
        for (int i = 0; i < characterPickerManager.characterSelectors.Count; i++)
        {
            if (characterPickerManager.characterSelectors[i].unlocked)
            {
                string characterName = characterPickerManager.characterSelectors[i].playerMenu.characterStats.characterName;
                int currentLevel = PlayerPrefs.GetInt(characterName + "_level", 0);
                int upgradeCostCoins = characterPickerManager.characterSelectors[i].playerMenu.characterStats.upgradeCostCoins * (currentLevel + 1);
                int upgradeCostMoney = characterPickerManager.characterSelectors[i].playerMenu.characterStats.upgradeCostMoney * (currentLevel + 1);
                Debug.Log("Looking at Selector : " + characterPickerManager.characterSelectors[i].playerMenu.name + " Checking Upgrades for Character : " + characterName + ", at level : " + currentLevel);
                Debug.Log("Character = " + characterPickerManager.characterSelectors[i].name + " is upgradeable = " + CheckPrice(upgradeCostCoins, upgradeCostMoney));
                if (CheckPrice(upgradeCostCoins, upgradeCostMoney))
                {
                    Debug.Log("-Checking for upgrades, current level is : " + currentLevel);
                    if (currentLevel < 6)
                    {
                        Debug.Log("-Upgrade-Character is upgradeable, toggling notif ON");
                        characterPickerManager.characterSelectors[i].upgradeableImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.Log("-Upgrade-Character is maxed, toggling notif OFF");
                        characterPickerManager.characterSelectors[i].upgradeableImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.Log("-Upgrade- Not enough currency");

                    characterPickerManager.characterSelectors[i].upgradeableImage.gameObject.SetActive(false);
                }
            }
        }
    }
    public void CheckIfUpgradesAreAvailableForCurrentCharacter(CharacterSelector characterSelector)
    {
        if (characterSelector.unlocked)
        {
            string characterName = characterSelector.playerMenu.characterStats.characterName;
            int currentLevel = PlayerPrefs.GetInt(characterName + "_level", 0);
            int upgradeCostCoins = characterSelector.playerMenu.characterStats.upgradeCostCoins * (currentLevel + 1);
            int upgradeCostMoney = characterSelector.playerMenu.characterStats.upgradeCostMoney * (currentLevel + 1);
            if (CheckPrice(upgradeCostCoins, upgradeCostMoney))
            {
                Debug.Log("-Checking for upgrades, current level is : " + currentLevel);
                if (currentLevel < 5)
                {
                    characterSelector.upgradeableImage.gameObject.SetActive(true);
                }
            }
            else characterSelector.upgradeableImage.gameObject.SetActive(false);
        }
    }
    private bool CheckPrice(int coins, int money)
    {
        Debug.Log("Player coins  = " + PlayerPrefs.GetInt("coins", 0) + "Cost coins = " + coins + " |||| " + "Player Money = " + PlayerPrefs.GetInt("money", 0) + " Cost money = " + money);
        if ((PlayerPrefs.GetInt("coins", 0) >= coins) && (PlayerPrefs.GetInt("money", 0) >= money))
            return true;
        else return false;
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
