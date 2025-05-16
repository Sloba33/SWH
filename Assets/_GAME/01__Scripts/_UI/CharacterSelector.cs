using UnityEngine.UI;
using UnityEngine;
using Coffee.UIEffects;

public class CharacterSelector : MonoBehaviour
{
    public bool isEditable;
    public bool isAdReward;
    public CharacterStats characterStats;
    public HelmetItemManager helmetItemManager;
    public CharacterPickerManager characterPickerManager;
    public PlayerMenu playerMenu;
    public Button button;
    public CharacterType characterType;
    public GameObject lockImage;
    public Image characterPortrait;
    public Color lockedColor;
    public Color unlockedColor;
    public bool unlocked;
    public UIShadow uiShadow;
    public GameObject notificationImage;
    public CustomizationPanelManager customizationPanelManager;
    private void Awake()
    {
        if (characterStats == null) return;
        if (button == null) button = GetComponent<Button>();
        uiShadow = GetComponent<UIShadow>();
        if (characterPickerManager.currentCharacter != this)
            uiShadow.enabled = false;
        else uiShadow.enabled = true;
        unlocked = PlayerPrefs.GetInt(characterStats.characterName) == 1;
        Debug.Log("Set unlocked to : " + unlocked + " for character: " + characterStats.characterName);

        button.onClick.AddListener(() =>
        {
            characterPickerManager.SetCharacter(this, false);
            CharacterManager.Instance.PlayClick();
            SetCharacterSelector();

        });

        if (unlocked)
        {
            bool characterNotificationClicked = PlayerPrefs.GetInt(characterStats.characterName.ToString() + "_clicked") == 0;
            if (characterNotificationClicked)
            {
                Debug.Log("Item clicked? :" + PlayerPrefs.GetInt(characterStats.characterName + "_clicked"));
                if (characterStats.characterName != "Toby")
                    notificationImage.SetActive(true);

            }
            else notificationImage.SetActive(false);
            LockCharacter(false);
        }
        else
        {
            LockCharacter(true);
        }
        customizationPanelManager = FindObjectOfType<CustomizationPanelManager>();
    }
    public void SetCharacterSelector()
    {
        CharacterManager.Instance.currentCharacterSelector = this;
    }
    public void LockCharacter(bool flag)
    {
        if (lockImage != null)
            lockImage.SetActive(flag);
        characterPortrait.color = flag ? lockedColor : unlockedColor;
        bool characterNotificationClicked = PlayerPrefs.GetInt(characterStats.characterName.ToString() + "_clicked") == 0;
        if (characterNotificationClicked && unlocked && characterStats.characterName != "Toby")
        {
            Debug.Log("Item clicked? :" + PlayerPrefs.GetInt(characterStats.characterName + "_clicked"));
            notificationImage.SetActive(true);

        }
        else notificationImage.SetActive(false);

    }
}

