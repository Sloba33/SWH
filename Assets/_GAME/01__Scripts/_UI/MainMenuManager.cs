using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Eflatun.SceneReference;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public LevelSelectionManager levelSelectionManager;
    public Image workersUnlockedNotification;
    public Image upgradeAvailableNotification;
    public TMP_InputField inputField;
    public TextMeshProUGUI inputField_Text;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI characterName;
    public GameObject spotLight,
        profileCamera,
        floor,
        backgroundPanel,
        particles,
        box;
    public Transform boxLeftDefaultPos,
        boxRightDefaultPos;
    public GameObject settingsPanel,
        workButton,
        campaignButton;
    public GameObject weaponsParentObject;
    public List<GameObject> characters = new();
    public GameObject boxLeft,
        boxRight;
    public bool isCharacterPicking;
    public PlayerMenu currentCharacter;
    public List<GameObject> allPanels = new();
    public GameObject mainMenu,
        customizationPanel,
        shop,
        singlePlayer,
        multiPlayer,
        backButton_MainMenu,
        backButton_Customization;
    public CustomizationPanelManager customizationPanelManager;
    public Transform cameraColor,
        cameraMain,
        cameraWeapon,
        cameraCharacter,
        cameraHelmet,
        cameraCustomize;
    bool isZoomed;
    private bool isBoxMovedOut;
    public Transform mainCamera;
    public Collider characterSelectorCollider;
    [Header("Stats")]
    public Image strengthFillBar;       // Fill bar for strength
    public Image speedFillBar;          // Fill bar for speed
    public Image specialFillBar;          // Fill bar for speed
    public Button BackgroundContinueButton;
    public List<GameObject> levelBars = new();
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI priceTextCoins, priceTextMoney, priceTextGems;

    public TextMeshProUGUI strengthStatText, speedStatText, specialStatText;

    public ImageGallery imageGallery;
    public GameObject xpFillBar;
    public void UpdateCurrency()
    {
        CharacterManager.Instance.CheckIfUpgradesAreAffordable();
    }
    private void Awake()
    {
        PlayerPrefs.SetInt("Toby", 1);
        // PlayerPrefs.SetInt("Weapon_Pickaxe", 1);
        PlayerPrefs.SetInt("Helmet_Standard", 1);
        // PlayerPrefs.SetInt("gems", 1000);
        gemsText.text = PlayerPrefs.GetInt("gems").ToString();


    }
    public TextMeshProUGUI coinText,
        moneyText,
        gemsText;



    public void OnNameEntered(string name)
    {
        PlayerPrefs.SetString("playerName", name);
        inputField_Text.text = name;
        inputField.text = name;
        playerName.text = name;
    }
    public void EnableWorkersNotification(bool flag)
    {
        Debug.Log("Enable Worker Notification : " + flag);
        if (workersUnlockedNotification != null) workersUnlockedNotification.gameObject.SetActive(flag);
    }
    public void OpenProfileUI()
    {
        CharacterManager.Instance.currentCharacter.canBlink = false;
        // CharacterManager.Instance.currentCharacter.hasBlinked = false;

        box.SetActive(false);
        spotLight.SetActive(false);
        particles.SetActive(false);
        mainMenu.SetActive(false);
        mainCamera.GetComponent<Camera>().enabled = false;
        profileCamera.GetComponent<Camera>().enabled = true;
        floor.SetActive(false);
        backgroundPanel.SetActive(true);
        CharacterManager.Instance.currentCharacter.playerStand.SetActive(true);
        CharacterManager.Instance.currentCharacter.GetComponent<Animator>().SetBool("Profile", true);
        CharacterManager.Instance.currentCharacter.GetComponent<Animator>().SetBool("Picking", true);
    }
    public void OpenLevelSelectionMenu()
    {
        levelSelectionManager.OpenLevelSelection();
    }

    // When closing level selection menu  

    public void CloseProfileUI()
    {
        CharacterManager.Instance.currentCharacter.canBlink = true;
        spotLight.SetActive(true);
        box.SetActive(true);
        mainMenu.SetActive(true);
        particles.SetActive(true);
        profileCamera.GetComponent<Camera>().enabled = false;
        mainCamera.GetComponent<Camera>().enabled = true;
        floor.SetActive(true);
        backgroundPanel.SetActive(false);
        CharacterManager.Instance.currentCharacter.playerStand.SetActive(false);
        CharacterManager.Instance.currentCharacter.GetComponent<Animator>().SetBool("Profile", false);
        CharacterManager.Instance.currentCharacter.GetComponent<Animator>().SetBool("Picking", false);
    }

    private void Start()
    {
        PlayerPrefs.SetInt("ShouldAddXP", 1);
        if (PlayerPrefs.GetInt("ShouldAddXP") != 0)
        {
            AddXP();
            PlayerPrefs.SetInt("ShouldAddXP", 0);
        }
        inputField.onEndEdit.AddListener(OnNameEntered);
        playerName.text = PlayerPrefs.GetString("playerName", "Player");
        inputField_Text.text = PlayerPrefs.GetString("playerName", "Player");
        inputField.text = PlayerPrefs.GetString("playerName");
        // yield return new WaitForSeconds(1.2f);
        customizationPanelManager.SetCurrentCharacter(currentCharacter);
        // customizationPanelManager.UpdateDefaultColors();
        if (PlayerPrefs.GetInt("FirstTimeMenu") < 2)
        {
            BackgroundContinueButton.gameObject.SetActive(false);
        }
        imageGallery.PopulateOnLoad();
    }

    public ParticleSystem xpPrefab;
    public Transform xpTarget;
    public RectTransform startPos;

    public void AddXP()
    {
        for (int i = 0; i < 5; i++)
        {
            // ParticleSystem xp = Instantiate(xpPrefab, startPos);
            // xp.GetComponent<ParticleFollowTest>().target = xpTarget;
        }
    }

    public void TurnOffPanels(bool state)
    {
        foreach (GameObject pane in allPanels)
        {
            pane.SetActive(state);
        }
        backButton_Customization.SetActive(state);
    }

    public void CustomizeColors()
    {
        backButton_Customization.SetActive(!backButton_Customization.activeSelf);
        mainMenu.SetActive(!mainMenu.activeSelf);
        customizationPanel.SetActive(!customizationPanel.activeSelf);
        if (!isZoomed)
        {
            isZoomed = true;
            mainCamera.DOMove(cameraColor.position, 1f).Play();
            mainCamera.DORotate(cameraColor.transform.rotation.eulerAngles, 1f).Play();
        }
        else
        {
            isZoomed = false;
            mainCamera.DOMove(cameraMain.position, 1f).Play();
            mainCamera.DORotate(cameraMain.transform.rotation.eulerAngles, 1f).Play();
        }
    }

    private bool firstLoad;

    public void OpenCustomizationPanel()
    {
        backButton_Customization.SetActive(false);
        customizationPanelManager.TurnOnTabs();
        customizationPanel.SetActive(true);
        mainCamera.DOMove(cameraCustomize.position, 1f).Play();
        mainCamera.DORotate(cameraCustomize.transform.rotation.eulerAngles, 1f).Play();
        MoveBoxes(true, 0.75f);
        // weaponsParentObject.SetActive(true);
        currentCharacter.GetComponent<Animator>().SetBool("Picking", true);
        characterSelectorCollider.enabled = false;
        // customizationPanelManager.customizationTabs[0].GetComponent<Button>().onClick.Invoke();
        customizationPanelManager.customizationTabs[0].SelectTab();

        CloseMainMenu();
        backButton_MainMenu.SetActive(true);
        // if (!firstLoad)
        // {
        //     customizationPanelManager.SetStartingScales();
        //     firstLoad = true;
        // }
    }

    public void CloseCustomizationPanel()
    {
        backButton_MainMenu.SetActive(false);
        backButton_Customization.SetActive(false);
        customizationPanel.SetActive(false);
        mainCamera.DOMove(cameraMain.position, 1f).Play();
        mainCamera.DORotate(cameraMain.transform.rotation.eulerAngles, 1f).Play();
        currentCharacter.weaponsInHand.gameObject.SetActive(false);
        currentCharacter.GetComponent<Animator>().SetBool("Picking", false);
        characterSelectorCollider.enabled = true;
        MoveBoxes(false, 0.75f);
    }

    public void ToggleBackButton()
    {
        backButton_Customization.SetActive(!backButton_Customization.activeSelf);
        customizationPanel.SetActive(!customizationPanel.activeSelf);
        mainMenu.SetActive(!mainMenu.activeSelf);
        isZoomed = false;
        mainCamera.DOMove(cameraMain.position, 1f).Play();
        mainCamera.DORotate(cameraColor.transform.rotation.eulerAngles, 1f).Play();
    }

    public void OpenMainMenu()
    {
        customizationPanelManager.currentTab = null;
        CharacterManager.Instance.RevertSelectionStates();
        mainMenu.SetActive(true);
        CloseCustomizationPanel();
        MoveBoxes(false, 0.75f);
    }

    public void CloseMainMenu()
    {
        mainMenu.SetActive(false);
        customizationPanel.SetActive(true);
        backButton_Customization.SetActive(false);
    }

    public void OpenWeaponsPanel()
    {
        WeaponsPanelClickOpen();
    }

    public GameObject weaponsPanel;

    public void WeaponsPanelClickOpen()
    {
        SetWeaponCamera();
        foreach (CustomizationTab tab in customizationPanelManager.customizationTabs)
        {
            tab.gameObject.SetActive(false);
        }
        CloseMainMenu();
        currentCharacter.GetComponent<Animator>().SetBool("Picking", true);
        weaponsPanel.SetActive(true);
        backButton_MainMenu.SetActive(true);
        customizationPanel.SetActive(true);
        MoveBoxes(true, 0.75f);
    }

    public void SetWeaponCamera()
    {
        mainCamera.DOMove(cameraWeapon.position, 1f).Play();
        mainCamera.DORotate(cameraWeapon.transform.rotation.eulerAngles, 1f).Play();
        // weaponsParentObject.SetActive(true);
    }

    public void SetCustomizationCamera()
    {
        mainCamera.DOMove(cameraCustomize.position, 1f).Play();
        mainCamera.DORotate(cameraCustomize.transform.rotation.eulerAngles, 1f).Play();
    }

    public void SetColorCamera()
    {
        mainCamera.DOMove(cameraColor.position, 1f).Play();
        mainCamera.DORotate(cameraColor.transform.rotation.eulerAngles, 1f).Play();
    }

    public void SetCharacterCamera()
    {
        mainCamera.DOMove(cameraCharacter.position, 1f).Play();
        mainCamera.DORotate(cameraCharacter.transform.rotation.eulerAngles, 1f).Play();
        isCharacterPicking = true;
    }

    public void SetHelmetCamera()
    {
        mainCamera.DOMove(cameraHelmet.position, 1f).Play();
        mainCamera.DORotate(cameraHelmet.transform.rotation.eulerAngles, 1f).Play();
    }

    public void MoveBoxes(bool moveOut, float duration)
    {
        if (moveOut && !isBoxMovedOut)
        {
            Vector3 leftPos = boxLeft.transform.position - new Vector3(0, 0, 5f);
            Vector3 rightPos = boxRight.transform.position + new Vector3(0, 0, 5f);
            boxLeft.transform.DOMove(leftPos, duration).Play();
            boxRight.transform.DOMove(rightPos, duration).Play();
            isBoxMovedOut = true;
        }
        else if (!moveOut && isBoxMovedOut)
        {
            Vector3 leftPos = boxLeft.transform.position + new Vector3(0, 0, 5f);
            Vector3 rightPos = boxRight.transform.position - new Vector3(0, 0, 5f);
            boxLeft.transform.DOMove(boxLeftDefaultPos.position, duration).Play();
            boxRight.transform.DOMove(boxRightDefaultPos.position, duration).Play();
            isBoxMovedOut = false;
        }
    }


    private bool isOpen = false;
    private bool _isSettingsAnimating = false; // NEW: Flag to prevent re-triggering during animation
    public void OpenSettings()
    {
        if (_isSettingsAnimating) return; // Prevent opening if an animation is already playing
        _isSettingsAnimating = true; // Set flag to indicate animation has started

        settingsPanel.SetActive(true);
        // Explicitly play the "SettingsFadeIn" animation.
        // Make sure this animation exists in your Animator Controller for settingsPanel.
        // If your "open" animation is just the default entry state, this might not be strictly needed,
        // but it's good practice for clarity and to ensure the animation is triggered.
        if (settingsPanel.gameObject.activeSelf)
        {
            Animator settingsAnimator = settingsPanel.GetComponent<Animator>();
            if (settingsAnimator != null)
            {
                // Ensure "SettingsFadeIn" is the correct name of your opening animation clip.
                // You might need to get the actual clip length for a more precise wait time.
                settingsAnimator.Play("SettingsFadeIn");
            }
        }

        // Immediately hide other main menu buttons
        if (workButton != null) workButton.gameObject.SetActive(false);
        if (campaignButton != null) campaignButton.gameObject.SetActive(false);

        // Assuming CharacterManager.Instance is correctly set up as a singleton
        if (CharacterManager.Instance != null) CharacterManager.Instance.PlayClick();

        isOpen = true; // Update state immediately

        // Start a coroutine to wait for the open animation to finish
        StartCoroutine(WaitForSettingsAnimation(true));
    }

    public void CloseSettings()
    {
        if (_isSettingsAnimating) return; // Prevent closing if an animation is already playing
        _isSettingsAnimating = true; // Set flag to indicate animation has started

        StartCoroutine(CloseSettingsCoroutine());
    }

    public IEnumerator CloseSettingsCoroutine()
    {
        // Immediately show other main menu buttons
        if (workButton != null) workButton.gameObject.SetActive(true);
        if (campaignButton != null) campaignButton.gameObject.SetActive(true);

        // Assuming CharacterManager.Instance is correctly set up as a singleton
        if (CharacterManager.Instance != null) CharacterManager.Instance.PlayClick();

        isOpen = false; // Update state immediately for next ControlSettings call

        if (settingsPanel.gameObject.activeSelf)
        {
            Animator settingsAnimator = settingsPanel.GetComponent<Animator>();
            if (settingsAnimator != null)
            {
                settingsAnimator.Play("SettingsFadeOut");
            }
        }

        // Wait for the animation to complete before deactivating the panel
        // IMPORTANT: Adjust this wait time to exactly match your "SettingsFadeOut" animation duration.
        // You can get this programmatically using AnimatorStateInfo if your animations change.
        yield return new WaitForSeconds(0.2f); // Existing wait time, confirm it matches your animation!

        settingsPanel.SetActive(false);
        _isSettingsAnimating = false; // Animation complete, allow further interaction
    }

    // NEW: Generic coroutine to wait for settings panel animations
    private IEnumerator WaitForSettingsAnimation(bool isOpening)
    {
        // You might get the actual animation length from the Animator:
        // AnimatorStateInfo stateInfo = settingsPanel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
        // yield return new WaitForSeconds(stateInfo.length);

        // For simplicity, using a hardcoded time that matches your animation duration.
        // Adjust this to the actual duration of your "SettingsFadeIn" animation.
        yield return new WaitForSeconds(0.2f); // Example duration, ensure it matches your animation

        // In OpenSettings, we set settingsPanel.SetActive(true) immediately.
        // The _isSettingsAnimating flag ensures no new clicks interfere until animation is done.
        _isSettingsAnimating = false;
    }
    [SerializeField] private SceneLoader sceneLoader;

    [SerializeField]
    private SceneReference multiplayerMatchmakingScene;
    public void StartNextLevel()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadNextJob();
        }
        else
        {
            Debug.LogError("SceneLoader is not assigned or found. Cannot load next job for intro levels.");
        }
    }
    public void HandleMainMenuWorkButton()
    {
        int currentLevel = PlayerPrefs.GetInt("Level", 0); // Get current level, default to 0 if not set

        // If the player hasn't completed all intro levels yet (Level < 3),
        // the work button should behave as it did before (load the next sequential level).
        if (currentLevel < 3)
        {
            if (sceneLoader != null)
            {
                // Call the existing method to load the next job/level automatically.
                // This assumes your SceneLoader.cs has a public method LoadNextJob()
                // that handles the logic of loading PlayerPrefs.GetInt("Level") + 3.
                sceneLoader.LoadNextJob();
            }
            else
            {
                Debug.LogError("SceneLoader is not assigned or found. Cannot load next job for intro levels.");
            }
        }
        // If the player has completed all intro levels (Level is 3 or more),
        // the work button should now display the Level Selector screen.
        else // currentLevel >= 3
        {
            if (levelSelectionManager != null)
            {
                // Ensure the LevelSelectionManager GameObject is active before attempting to open its UI.
                if (!levelSelectionManager.gameObject.activeSelf)
                {
                    levelSelectionManager.gameObject.SetActive(true);
                    OpenLevelSelectionMenu();
                }

                levelSelectionManager.OpenLevelSelection();
            }
            else
            {
                Debug.LogError("LevelSelectionManager is not assigned or found. Cannot open level selection.");
            }
        }
    }
    public void ControlSettings()
    {
        // Only proceed if no settings animation is currently playing
        if (!_isSettingsAnimating)
        {
            if (!isOpen)
                OpenSettings();
            else
                CloseSettings();
        }
        // If _isSettingsAnimating is true, the click is ignored, preventing spam issues.
    }

    public void CloseSettingsPanel()
    {
        // This is typically called from a "Back" button inside the settings panel.
        // It should also respect the animation flag to avoid conflicts.
        CloseSettings();
    }
    public void ResetGame()
    {
        PlayerPrefs.DeleteAll();

        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();
        Application.Quit();
    }
    
    public void StartMultiplayer()
    {
        if(multiplayerMatchmakingScene == null)
        {
            Debug.LogError("MultiplayerMatchmakingScene is not assigned");
            return;
        }
        
        if(multiplayerMatchmakingScene.UnsafeReason != SceneReferenceUnsafeReason.None)
        {
            Debug.LogError("MainMenuManager: multiplayerMatchmakingScene has a problem: " + multiplayerMatchmakingScene.UnsafeReason);
            return;
        }

        if (sceneLoader != null)
        {
            sceneLoader.LoadSceneByName(multiplayerMatchmakingScene.Name);
        }
        else
        {
            SceneManager.LoadScene(multiplayerMatchmakingScene.Name);
        }
    }
}
