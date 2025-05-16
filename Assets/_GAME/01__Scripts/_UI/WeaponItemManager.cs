using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponItemManager : MonoBehaviour
{
    public GameEvent weaponSetterEvent;

    public List<WeaponItem> weaponItems = new();
    public List<GameObject> weaponsAtBox = new();
    public List<GameObject> weaponsInHand = new();
    public WeaponItem weaponItem;
    public Transform content;
    int kek;
    [Header("Weapon Stats")]
    public Image hitsFillBar;
    public Image energyRechargeBar;
    public TextMeshProUGUI weaponName;
    public Transform weaponContentPanel;
    public Button purchaseButton;
    public Image currencyImage;
    public TextMeshProUGUI priceText;

    private void Start()
    {
        SetWeaponStats();
    }
    public void SetWeaponStats()
    {

        if (weaponItem != null)
        {
            Debug.Log("WeaponItem name : " + weaponItem.weaponName);
            weaponName.text = weaponItem.weaponName;
            hitsFillBar.fillAmount = 10 / weaponItem.weaponToSpawn.energyConsumption;
            energyRechargeBar.fillAmount = weaponItem.weaponToSpawn.energyRecharge * 0.2f;
            priceText.text = weaponItem.weaponPrice + "";
        }
        else Debug.LogError("Weapon item null");
    }
    public void SetReferencesStart()
    {
        for (int i = 0; i < content.childCount; i++)
        {
            if (content.GetChild(i).GetComponent<Button>().interactable)
            {
                kek++;
            }
        }
        for (int i = 0; i < kek; i++)
        {
            weaponItems.Add(content.GetChild(i).GetComponent<WeaponItem>());
            weaponItems[i].weaponItemManager = this;

        }
        int id = PlayerPrefs.GetInt("SelectedWeaponID", 0);
        weaponItem = weaponItems[id];
        // SelectWeapon(weaponItem);
        SetStartingCheckmarks();
    }
    public WeaponItem FindWeaponByID()
    {
        int index = PlayerPrefs.GetInt("SelectedWeaponID", 0);
        return weaponItem = weaponItems[index];
    }
    private UIShadow uiShadow;
    public void SelectWeapon(WeaponItem weaponItem)
    {
        if (uiShadow != null) uiShadow.enabled = false;
        purchaseButton.onClick.RemoveAllListeners();
        if (!weaponItem.unlocked)
        {

            purchaseButton.gameObject.SetActive(true);
            purchaseButton.onClick.AddListener(() =>
                              {
                                  PurchaseWeapon(weaponItem);
                              });
            priceText.text = weaponItem.weaponPrice + "";
            if (PlayerPrefs.GetInt("gems") < weaponItem.weaponPrice)
            {
                priceText.color = Color.red;
            }
            else priceText.color = Color.white;
        }
        else purchaseButton.gameObject.SetActive(false);
        this.weaponItem = weaponItem;
        SetWeaponStats();

        if (weaponItem.unlocked)
            PlayerPrefs.SetInt("SelectedWeaponID", weaponItem.id);
        weaponItem.uiShadow.enabled = true;
        if (weaponItem.unlocked) weaponItem.uiShadow.effectColor = Color.green;
        else weaponItem.uiShadow.effectColor = Color.red;
        uiShadow = weaponItem.uiShadow;

        if (PlayerPrefs.GetInt(weaponItem.weaponType + "_clicked") != 1)
        {
            PlayerPrefs.SetInt(weaponItem.weaponType + "_clicked", 1);
            weaponItem.notificationImage.SetActive(false);
        }
        weaponSetterEvent.Raise(this, weaponItem);
        SetStartingCheckmarks();

    }
    public void PurchaseWeapon(WeaponItem weaponItem)
    {
        Debug.Log("Weapon name: " + weaponItem.name);
        if (PlayerPrefs.GetInt("gems") >= weaponItem.weaponPrice)
        {
            PlayerPrefs.SetInt(weaponItem.weaponType.ToString(), 1);
            purchaseButton.gameObject.SetActive(false);
            weaponItem.unlocked = true;
            weaponItem.LockWeapon(false);
            PlayerPrefs.SetInt("gems", PlayerPrefs.GetInt("gems") - weaponItem.weaponPrice);
            SelectWeapon(weaponItem);
        }
        else
            Debug.Log(" NO MONEY ");

    }
    public void ToggleWeaponVisibility(int index, bool flag)
    {
        weaponsAtBox[index].SetActive(flag);
        weaponsInHand[index].SetActive(flag);
    }
    public int weaponCheckmarkIndex;
    public GameObject checkmarkPrefab;
    public void SetStartingCheckmarks()
    {
        weaponCheckmarkIndex = PlayerPrefs.GetInt("SelectedWeaponID", 0);
        if(content.GetChild(weaponCheckmarkIndex).GetComponent<WeaponItem>().unlocked)
        {
            checkmarkPrefab.SetActive(true);
        }
        else checkmarkPrefab.SetActive(false);
        Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
        checkmarkPrefab.GetComponent<RectTransform>().SetParent(content.GetChild(weaponCheckmarkIndex));
        checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;
        Debug.Log("Setting weaponCheckmarkIndex to : " + PlayerPrefs.GetInt("SelectedWeaponID", 0));
    }
    public void RemoveShadows()
    {
        for (int i = 0; i < weaponItems.Count; i++)
        {
            if (weaponItems[i].GetComponent<UIShadow>() != null)
                weaponItems[i].uiShadow.enabled = false;
        }
    }
}
