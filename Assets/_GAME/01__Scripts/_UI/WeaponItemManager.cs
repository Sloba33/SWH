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
    public GameObject statsPanel;

    private void Start()
    {
        SetWeaponStats();
        ToggleWeaponInteractability();
        currentlySelectedWeapon = CharacterManager.Instance.currentWeapon.GetComponent<WeaponItem>();
    }
    private void ToggleWeaponInteractability()
    {
        if (PlayerPrefs.GetInt("AnyWeaponsUnlocked", 0) == 0)
        {
            for (int i = 0; i < weaponItems.Count; i++)
            {
                weaponItems[i].GetComponent<Button>().interactable = false;
                statsPanel.SetActive(false);
            }
        }

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
            if (PlayerPrefs.GetInt("gems") < weaponItem.weaponPrice)
            {
                priceText.color = Color.red;
            }
            else priceText.color = Color.white;
        }
        else Debug.LogError("Weaponitem null");
    }
    public void SetWeaponStats(WeaponItem wepItem)
    {
        if (weaponItem.unlocked) purchaseButton.gameObject.SetActive(false);
        Debug.Log("WeaponItem name : " + wepItem.weaponName);
        weaponName.text = wepItem.weaponName;
        hitsFillBar.fillAmount = 10 / wepItem.weaponToSpawn.energyConsumption;
        energyRechargeBar.fillAmount = wepItem.weaponToSpawn.energyRecharge * 0.2f;
        priceText.text = wepItem.weaponPrice + "";
        if (PlayerPrefs.GetInt("gems") < wepItem.weaponPrice)
        {
            priceText.color = Color.red;
        }
        else priceText.color = Color.white;
    }
    public void CheckWeaponUpgradePurchaseButton()
    {
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
        if (weaponItem.isTrophyRoadItem)
        {
            Debug.Log("Is trophy road item");
            if (weaponItem.unlocked)
            {
                purchaseButton.gameObject.SetActive(false);

            }
            else
            {
                purchaseButton.gameObject.SetActive(false);

            }
        }
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

        // SelectWeapon(weaponItem); // testing purpose << 
        Debug.Log("Selected Weapon ID at start: " + id);
    }
    public WeaponItem FindWeaponByID()
    {
        int index = PlayerPrefs.GetInt("SelectedWeaponID", 0);
        return weaponItem = weaponItems[index];
    }
    private UIShadow uiShadow;
    public WeaponItem previouslySelectedWeapon;
    public WeaponItem currentlySelectedWeapon;
    public void SelectWeapon(WeaponItem weaponItem)
    {

        if (uiShadow != null) uiShadow.enabled = false;
        purchaseButton.onClick.RemoveAllListeners();

        if (!weaponItem.unlocked)
        {
            currentlySelectedWeapon = weaponItem;
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
        if (weaponItem.isTrophyRoadItem)
        {
            if (weaponItem.unlocked)
            {
                purchaseButton.gameObject.SetActive(false);

            }
            else
            {
                purchaseButton.gameObject.SetActive(false);

            }
        }


        if (weaponItem.unlocked)
        {
            currentlySelectedWeapon = weaponItem;
            previouslySelectedWeapon = weaponItem;
            Debug.Log("Weapon is unlocked, showing upgrade button");
            purchaseButton.gameObject.SetActive(false);
            // upgradeButton.gameObject.SetActive(true);
            PlayerPrefs.SetInt("SelectedWeaponID", weaponItem.id);
        }
        else
        {
            Debug.Log("Weapon is locked, showing purchase button");

            purchaseButton.gameObject.SetActive(true);
        }
        this.weaponItem = weaponItem;
        SetWeaponStats();
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
            PlayerPrefs.SetInt("AnyWeaponsUnlocked", 1);
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
        if (PlayerPrefs.GetInt("AnyWeaponsUnlocked") == 0) return;
        weaponCheckmarkIndex = PlayerPrefs.GetInt("SelectedWeaponID", 0);
        if (content.GetChild(weaponCheckmarkIndex).GetComponent<WeaponItem>().unlocked)
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
