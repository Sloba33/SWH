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
        Debug.Log("Selecting tab");
        Debug.Log("Current character that the tab sees :" + customizationPanelManager.currentCharacter.characterStats.characterName);
        customizationPanelManager.SelecTab(this);
        HighlightSeletedButton(this);
        if (tabType == TabType.Weapon)
        {
            Debug.Log("Weapon tab selected, enabling weapons in hand"); 
            ToggleWeaponsInHand(true);
            customizationPanelManager.mainMenuManager.SetWeaponCamera();
        }
        else if (tabType == TabType.Color)
        {
            Debug.Log("Color tab selected, disabling weapons in hand");
            ToggleWeaponsInHand(false);
            customizationPanelManager.mainMenuManager.SetColorCamera();
        }
        else if (tabType == TabType.Character)
        {
            Debug.Log("Character tab selected, disabling weapons in hand");
            ToggleWeaponsInHand(false);
            customizationPanelManager.mainMenuManager.SetCharacterCamera();
            HighlightSeletedButton(this);
        }
        else if (tabType == TabType.Helmet)
        {
            Debug.Log("Helmet tab selected, disabling weapons in hand");
            ToggleWeaponsInHand(false);
            customizationPanelManager.mainMenuManager.SetHelmetCamera();
        }
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
        customizationPanelManager.currentCharacter.weaponsInHand.gameObject.SetActive(flag);
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
