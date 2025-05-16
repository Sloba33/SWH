using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationPanelManager : MonoBehaviour
{

    public GameObject weaponsInHand,
        weaponAtBox;
    public PlayerMenu currentCharacter;
    public MainMenuManager mainMenuManager;
    public List<CustomizationTab> customizationTabs = new();
    public List<GameObject> customizationPanels = new();
    public HelmetItemManager helmetItemManager;
    public List<ColorButtonManager> colorButtonManagers = new();
    public Color defaultShirtColor,
        defaultShirtSleeveColor,
        defaultBodyColor,
        defaultBodyPocketColor,
        defaultHatColor,
        defaultShoesColor;


    public void SetCurrentCharacter(PlayerMenu pm)
    {
        currentCharacter = pm;
        foreach (ColorButtonManager cbm in colorButtonManagers)
        {
            cbm.currentCharacter = pm;
            cbm.customizationPanelManager = this;
        }

    }

    public void ToggleTabs(bool state)
    {
        foreach (CustomizationTab tab in customizationTabs)
        {
            tab.gameObject.SetActive(state);
        }
    }

    private bool firstLoad;

    public void TurnOnTabs()
    {
        foreach (CustomizationTab tab in customizationTabs)
        {
            tab.gameObject.SetActive(true);
        }
        if (!firstLoad)
        {
            firstLoad = true;
            SetStartingScales();
        }
    }

    public void SelecTab(CustomizationTab activeTab)
    {
        foreach (CustomizationTab tab in customizationTabs)
        {
            if (tab == activeTab)
            {
                tab.targetPanel.SetActive(true);
            }
            else
            {
                tab.targetPanel.SetActive(false);
            }
        }
    }

    public void CloseAllTabs()
    {
        TurnOnTabs();
        mainMenuManager.OpenCustomizationPanel();
        CloseAllPanels();
        mainMenuManager.SetCustomizationCamera();
        mainMenuManager.isCharacterPicking = false;
    }

    public void CloseAllPanels()
    {
        foreach (GameObject panel in customizationPanels)
        {
            panel.gameObject.SetActive(false);
        }
    }
    public void ToggleEditingTabs(bool state)
    {
        foreach (CustomizationTab tab in customizationTabs)
        {
            if (tab.tabType == CustomizationTab.TabType.Color) tab.GetComponent<Button>().interactable = state;
            if (tab.tabType == CustomizationTab.TabType.Helmet) tab.GetComponent<Button>().interactable = state;  //consider disabling
        }
    }
    public void UpdateDefaultColors()
    {
        Debug.Log("Setting default colors for buttons");
        for (int i = 0; i < colorButtonManagers.Count; i++)
        {
            for (int j = 0; j < colorButtonManagers[i].colorButtons.Count; j++)
            {
                if (colorButtonManagers[i].colorButtons[j].isDefault)
                {
                    // Debug.Log("Setting color of : " + (colorButtonManagers[i].colorButtons[j].name + " To : " + mainMenuManager.playerMenu.defaultShirt.r));
                    if (colorButtonManagers[i].clothesType == ColorButtonManager.ClothesType.Shirt)
                    {
                        colorButtonManagers[i].colorButtons[j].color = mainMenuManager
                            .currentCharacter
                            .defaultShirt;
                        defaultShirtColor = mainMenuManager.currentCharacter.defaultShirt;
                        defaultShirtSleeveColor = mainMenuManager.currentCharacter.defaultShirt;
                        Debug.Log("Changing shirt color");
                    }
                    else if (
                        colorButtonManagers[i].clothesType == ColorButtonManager.ClothesType.Hat
                    )
                    {
                        Debug.Log(
                            "Updating hat color of :"
                                + colorButtonManagers[i].colorButtons[j].name
                                + " to : "
                                + mainMenuManager.currentCharacter.defaultHat
                        );
                        colorButtonManagers[i].colorButtons[j].color = mainMenuManager
                            .currentCharacter
                            .defaultHat;
                        defaultHatColor = mainMenuManager.currentCharacter.defaultHat;
                    }
                    else if (
                        colorButtonManagers[i].clothesType == ColorButtonManager.ClothesType.Shoes
                    )
                    {
                        colorButtonManagers[i].colorButtons[j].color = mainMenuManager
                            .currentCharacter
                            .defaultShoes;
                        defaultShoesColor = mainMenuManager.currentCharacter.defaultShoes;
                    }
                    else if (
                        colorButtonManagers[i].clothesType
                        == ColorButtonManager.ClothesType.Coveralls
                    )
                    {
                        colorButtonManagers[i].colorButtons[j].color = mainMenuManager
                            .currentCharacter
                            .defaultOutfit;
                        defaultBodyColor = mainMenuManager.currentCharacter.defaultOutfit;
                        Color pocketMaterial =
                            mainMenuManager.currentCharacter.defaultOutfit * (1 - 0.2f);
                    }
                }
            }
        }
        Debug.Log("Default colors updated");
    }

    public void StopPickingCharacter()
    {
        mainMenuManager.isCharacterPicking = false;
    }

    public void StartCharacterPicking()
    {
        mainMenuManager.isCharacterPicking = true;
    }

    public void StopAnimation()
    {
        mainMenuManager.currentCharacter.GetComponent<Animator>().SetBool("Picking", true);
    }

    public void EnableWeaponsInHand()
    {
        Debug.Log("Enabling weapons in hand");
        weaponsInHand.SetActive(true);
        weaponAtBox.SetActive(false);
    }

    public void SetCharacter(Component sender, object data)
    {
        currentCharacter = (PlayerMenu)data;
        for (int i = 0; i < colorButtonManagers.Count; i++)
        {
            colorButtonManagers[i].currentCharacter = currentCharacter;
        }
    }

    public void SetStartingScales()
    {
        foreach (CustomizationTab tab in customizationTabs)
        {
            tab.startingScale = tab.GetComponent<RectTransform>().localScale;
            tab.targetScale = new Vector3(
                tab.startingScale.x * 1.2f,
                tab.startingScale.y * 1.2f,
                tab.startingScale.z * 1.2f
            );
            Debug.Log("Starting scale for tab " + tab.name + "Is : " + tab.startingScale);
        }
    }
}
