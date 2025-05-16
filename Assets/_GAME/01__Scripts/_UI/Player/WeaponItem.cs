using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class WeaponItem : MonoBehaviour
{
    public string weaponName;
    public WeaponItemManager weaponItemManager;
    public Weapon weaponToSpawn;
    public GameObject weaponAtBox;
    public Button button;
    public int id;
    public Image weaponImage;
    public WeaponType weaponType;
    public GameObject lockImage;
    public GameObject notificationImage;
    public Color lockedColor, unlockedColor;
    private bool isPurchased;
    public bool unlocked;
    public Button purchaseButton;
    public int weaponPrice;
    public UIShadow uiShadow;
    private void Start()
    {
        
        if (button == null)
            button = GetComponent<Button>();
        id = transform.GetSiblingIndex();
        uiShadow = GetComponent<UIShadow>();
        uiShadow.enabled = false;
        unlocked = PlayerPrefs.GetInt(weaponType.ToString(), 0) == 1;

        Debug.Log("Weapon name : " + weaponType.ToString() + " unlocked " + unlocked);
        button.onClick.AddListener(() =>
                          {
                              weaponItemManager.SelectWeapon(this);
                              CharacterManager.Instance.PlayClick();

                          });
        if (unlocked)
        {
            if (PlayerPrefs.GetInt(weaponType.ToString() + "_clicked") == 0)
            {
                Debug.Log("Item clicked? :" + PlayerPrefs.GetInt(weaponType + "_clicked"));
                notificationImage.SetActive(true);

            }
            else notificationImage.SetActive(false);
            LockWeapon(false);
        }
        else
        {
            LockWeapon(true);
        }

    }
    public void LockWeapon(bool flag)
    {
        if (lockImage != null)
            lockImage.SetActive(flag);
        weaponImage.color = flag ? lockedColor : unlockedColor;

    }


}
