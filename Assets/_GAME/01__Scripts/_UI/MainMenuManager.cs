using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public Image workersUnlockedNotification;
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
        imageGallery.Initialize();
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

    private bool isOpen;

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        workButton.SetActive(false);
        campaignButton.SetActive(false);
        CharacterManager.Instance.PlayClick();
        isOpen = true;
    }

    public void CloseSettings()
    {
        StartCoroutine(CloseSettingsCoroutine());
    }

    public IEnumerator CloseSettingsCoroutine()
    {
        workButton.SetActive(true);
        campaignButton.SetActive(true);
        CharacterManager.Instance.PlayClick();
        isOpen = false;
        if (settingsPanel.gameObject.activeSelf)
            settingsPanel.GetComponent<Animator>().Play("SettingsFadeOut");
        yield return new WaitForSeconds(0.2f);

        settingsPanel.SetActive(false);
    }

    public void ControlSettings()
    {
        if (!isOpen)
            OpenSettings();
        else
            CloseSettings();
    }

    public void CloseSettingsPanel()
    {
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
}
