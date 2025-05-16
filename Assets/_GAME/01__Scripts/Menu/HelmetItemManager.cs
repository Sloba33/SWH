using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class HelmetItemManager : MonoBehaviour
{
    public GameEvent helmetSetterEvent;
    public List<HelmetItem> helmetItems = new();
    public HelmetItem helmetItem;
    public Helmet helmet;
    public Transform content;
    public PlayerMenu currentCharacter;
    [Header("Helmet Stats")]
    public Image durabilityFillBar;
    public TextMeshProUGUI helmetName;
    public TextMeshProUGUI priceText;
    private void Start()
    {

        int id = PlayerPrefs.GetInt("SelectedHelmetID", 0);
        helmetItem = helmetItems[id];
        helmet = CharacterManager.Instance.helmet;

        SetStartingCheckmarks();
        SetHelmetStats();

    }
    public void SetHelmetStats()
    {
        if (helmet != null)
        {
            helmetName.text = helmet.helmetName;
            durabilityFillBar.fillAmount = 0.1f * helmet.helmetDurability;
            priceText.text = helmetItem.helmetPrice + "";
        }
        else Debug.LogError("Weapon item null");
    }
    private UIShadow uiShadow;
    public Button purchaseButton;
    public void SelectHelmet(HelmetItem helmetItem)
    {
        if (uiShadow != null) uiShadow.enabled = false;
        purchaseButton.onClick.RemoveAllListeners();
        if (!helmetItem.unlocked)
        {
            purchaseButton.gameObject.SetActive(true);
            purchaseButton.onClick.AddListener(() =>
                              {
                                  PurchaseHelmet(helmetItem);
                              });
            priceText.text = helmetItem.helmetPrice + "";
            if (PlayerPrefs.GetInt("gems") < helmetItem.helmetPrice)
            {
                priceText.color = Color.red;
            }
            else priceText.color = Color.white;
        }
        else purchaseButton.gameObject.SetActive(false);
        this.helmetItem = helmetItem;
        this.helmet = helmetItem.helmet;
        SetHelmetStats();
        if (helmetItem.unlocked)
        {
            PlayerPrefs.SetInt("SelectedHelmetID", helmetItem.id);
        }
        helmetItem.uiShadow.enabled = true;
        if (helmetItem.unlocked) helmetItem.uiShadow.effectColor = Color.green;
        else helmetItem.uiShadow.effectColor = Color.red;
        uiShadow = helmetItem.uiShadow;

        if (PlayerPrefs.GetInt(helmetItem.helmetType + "_clicked") != 1)
        {
            PlayerPrefs.SetInt(helmetItem.helmetType + "_clicked", 1);

            helmetItem.notificationImage.SetActive(false);
        }
        helmetSetterEvent.Raise(this, helmetItem);
        SetStartingCheckmarks();
    }
    public void PurchaseHelmet(HelmetItem helmetItem)
    {
        Debug.Log("Weapon name: " + helmetItem.name);
        if (PlayerPrefs.GetInt("gems") >= helmetItem.helmetPrice)
        {
            PlayerPrefs.SetInt(helmetItem.helmetType.ToString(), 1);
            purchaseButton.gameObject.SetActive(false);
            helmetItem.unlocked = true;
            helmetItem.LockHelmet(false);
            PlayerPrefs.SetInt("gems", PlayerPrefs.GetInt("gems") - helmetItem.helmetPrice);
            SelectHelmet(helmetItem);
        }
        else
            Debug.Log(" NO MONEY ");

    }
    public void UnlockHelmet(Helmet helmet)
    {
        HelmetItem foundHelmetItem = FindHelmetItemByHelmet(helmet);
        PlayerPrefs.SetInt(foundHelmetItem.helmetType.ToString(), 1);
        purchaseButton.gameObject.SetActive(false);
        foundHelmetItem.unlocked = true;
        foundHelmetItem.LockHelmet(false);

        SelectHelmet(foundHelmetItem);
    }
    public HelmetItem FindHelmetItemByHelmet(Helmet helmet)
    {
        HelmetItem helmetItemToReturn = helmetItems[0];
        for (int i = 0; i < helmetItems.Count; i++)
        {
            if (helmetItems[i].helmet == helmet)
            {
                helmetItemToReturn = helmetItems[i];
            }

        }
        return helmetItemToReturn;
    }
    public HelmetItem FindHelmetByID()
    {
        int index = PlayerPrefs.GetInt("SelectedHelmetID", 0);
        return helmetItem = helmetItems[index];
    }
    public GameObject checkmarkPrefab;
    public int helmetCheckmarkIndex;
    public void SetStartingCheckmarks()
    {
        helmetCheckmarkIndex = PlayerPrefs.GetInt("SelectedHelmetID", 0);
        Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
        checkmarkPrefab.GetComponent<RectTransform>().SetParent(content.GetChild(helmetCheckmarkIndex));
        checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;
    }
    public void RemoveShadows()
    {
        for (int i = 0; i < helmetItems.Count; i++)
        {
            if (helmetItems[i].GetComponent<UIShadow>() != null)
                helmetItems[i].uiShadow.enabled = false;
        }
    }
}
