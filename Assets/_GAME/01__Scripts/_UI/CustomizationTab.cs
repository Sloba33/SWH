using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationTab : MonoBehaviour
{
    public GameObject targetPanel;
    public TabType tabType;
    public CustomizationPanelManager customizationPanelManager;
    public GameObject weaponInHandParent;
    public Vector3 startingScale;
    public Vector3 targetScale;
    private void Start()
    {
        GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                SelectTab();
            });
    }

    public void SelectTab()
    {
        if (customizationPanelManager.currentTab == this) return;
        Debug.Log("Selecting tab");

        // 🔑 Cancel helmet preview ONLY if we are leaving the Helmet tab


        Debug.Log("Current character that the tab sees : " +
                  customizationPanelManager.currentCharacter.characterStats.characterName);

        customizationPanelManager.SelecTab(this);
        HighlightSeletedButton(this);

        if (tabType == TabType.Weapon)
        {
            Debug.Log("Weapon tab selected, enabling weapons in hand");
            ToggleWeaponsInHand(true);
            WeaponItemManager weaponItemManager = FindObjectOfType<WeaponItemManager>();
            weaponItemManager.CheckWeaponUpgradePurchaseButton();
            CharacterManager.Instance.CancelHelmetPreview();
            customizationPanelManager.mainMenuManager.SetWeaponCamera();
        }
        else if (tabType == TabType.Color)
        {
            Debug.Log("Color tab selected, disabling weapons in hand");
            ToggleWeaponsInHand(false);
            CharacterManager.Instance.CancelHelmetPreview();
            customizationPanelManager.mainMenuManager.SetColorCamera();
            CharacterManager.Instance.RevertWeaponSelection();
        }
        else if (tabType == TabType.Character)
        {
            Debug.Log("Character tab selected, disabling weapons in hand");
            ToggleWeaponsInHand(false);
            CharacterManager.Instance.CancelHelmetPreview();
            customizationPanelManager.mainMenuManager.SetCharacterCamera();
            HighlightSeletedButton(this);
             CharacterManager.Instance.RevertWeaponSelection();
        }
        else if (tabType == TabType.Helmet)
        {
            Debug.Log("Helmet tab selected, disabling weapons in hand");
            ToggleWeaponsInHand(false);

            customizationPanelManager.mainMenuManager.SetHelmetCamera();
             CharacterManager.Instance.RevertWeaponSelection();
        }
        customizationPanelManager.currentTab = this;
    }


    public enum TabType
    {
        Weapon,
        Color,
        Character,
        Helmet
    }

    public bool selected;

    public void ToggleWeaponsInHand(bool flag)
    {
        if (PlayerPrefs.GetInt("AnyWeaponsUnlocked") == 0) Debug.LogWarning("Selecting weapon when none are unlocked");
        else customizationPanelManager.currentCharacter.weaponsInHand.gameObject.SetActive(flag);
    }

    private void HighlightSeletedButton(CustomizationTab ct)
    {
        for (int i = 0; i < customizationPanelManager.customizationTabs.Count; i++)
        {
            if (ct != customizationPanelManager.customizationTabs[i])
            {
                customizationPanelManager.customizationTabs[i].selected = false;
                customizationPanelManager.customizationTabs[i].transform.localScale =
                    customizationPanelManager.customizationTabs[i].startingScale;
                Debug.Log("Downsaling : " + customizationPanelManager.customizationTabs[i].name);
            }
        }

        ct.transform.localScale = targetScale;
        Debug.Log("Scaling : " + ct.gameObject.name);
    }
}
