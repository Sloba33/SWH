using System.Collections;
using System.Collections.Generic;
using DanielLochner.Assets.SimpleScrollSnap;
using UnityEngine;
using UnityEngine.UI;

public class ColorButtonManager : MonoBehaviour
{
    public GameObject checkmarkPrefab;

    public CustomizationPanelManager customizationPanelManager;
    public Transform contentObject;
    public List<ColorButton> colorButtons = new();
    public SimpleScrollSnap simpleScrollSnap;
    public SkinnedMeshRenderer bodyMesh, shoesMesh;
    public MeshRenderer hatMesh, hatLampMesh;
    public int[] materialIndexes;
    public bool isHat;
    public Material hatMat;
    public ClothesType clothesType;
    public PlayerMenu currentCharacter;
    public Color color;
    public int shirtColorIndex, overallsColorIndex, helmetColorIndex, shoesColorIndex;

    public enum ClothesType
    {
        Shirt, Coveralls, Hat, Shoes
    }
    private void Start()
    {
        SetStartingCheckmarks();
    }
    public void SetSpecificCheckmark()
    {
        Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
        checkmarkPrefab.GetComponent<RectTransform>().SetParent(contentObject.GetChild(0));
        checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;
    }
    public void ChangeColor(Color color, int index)
    {
        Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
        checkmarkPrefab.GetComponent<RectTransform>().SetParent(contentObject.GetChild(index));
        checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;

        if (clothesType == ClothesType.Shirt)
        {
            PlayerPrefs.SetInt("SelectedShirtColor", index);
            currentCharacter.ColorShirt(color);
        }
        if (clothesType == ClothesType.Coveralls)
        {
            PlayerPrefs.SetInt("SelectedOverallsColor", index);
            currentCharacter.ColorOveralls(color);
        }
        if (clothesType == ClothesType.Hat)
        {
            PlayerPrefs.SetInt("SelectedHelmetColor", index);
            currentCharacter.ColorHat(color);
        }
        if (clothesType == ClothesType.Shoes)
        {
            PlayerPrefs.SetInt("SelectedShoesColor", index);
            currentCharacter.ColorShoes(color);
        }

    }
    public static void SaveColor(string key, Color color)
    {
        PlayerPrefs.SetFloat(key + "R", color.r);
        PlayerPrefs.SetFloat(key + "G", color.g);
        PlayerPrefs.SetFloat(key + "B", color.b);

    }

    public void LoadColorButtons()
    {
        for (int i = 0; i < contentObject.childCount; i++)
        {
            colorButtons.Add(contentObject.GetChild(i).GetComponent<ColorButton>());
            colorButtons[i].colorButtonManager = this;
            if (!colorButtons[i].isDefault)
                colorButtons[i].color = colorButtons[i].transform.GetChild(0).GetComponent<Image>().color;
        }

    }
    public void SetStartingCheckmarks()
    {
        shirtColorIndex = PlayerPrefs.GetInt("SelectedShirtColor", 0);
        overallsColorIndex = PlayerPrefs.GetInt("SelectedOverallsColor", 0);
        helmetColorIndex = PlayerPrefs.GetInt("SelectedHelmetColor", 0);
        shoesColorIndex = PlayerPrefs.GetInt("SelectedShoesColor", 0);
        // Debug.Log("Shirt index : " + shirtColorIndex + " Overalls index :" + overallsColorIndex + " helmet index :" + helmetColorIndex + "shoes index :" + shoesColorIndex);


        if (clothesType == ClothesType.Shirt)
        {
            Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
            checkmarkPrefab.GetComponent<RectTransform>().SetParent(contentObject.GetChild(shirtColorIndex));
            checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;
            // currentCharacter.shirtMaterial.color = LoadColor("shirt");
            // Debug.Log("Current character :" + currentCharacter.name + " shirt material name :" + currentCharacter.shirtMaterial.name);
            if (currentCharacter.shirtMaterial != null) currentCharacter.shirtMaterial.color = colorButtons[shirtColorIndex].color;

            // Debug.Log("Set color of shirt to :" + colorButtons[shirtColorIndex].name);
            // Debug.Log("Loaded shirt color to prev");
        }
        if (clothesType == ClothesType.Coveralls)
        {
            Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
            checkmarkPrefab.GetComponent<RectTransform>().SetParent(contentObject.GetChild(overallsColorIndex));
            checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;
            if (currentCharacter.overallsMaterial != null) currentCharacter.overallsMaterial.color = colorButtons[overallsColorIndex].color;
            if (currentCharacter.overallsMaterial != null)
            {
                Color temp = currentCharacter.overallsMaterial.color;

                temp = temp * (1 - 0.2f);
                if (currentCharacter.overallsPocketMaterial != null) currentCharacter.overallsPocketMaterial.color = temp;
            }

            // Debug.Log("Set color of overalls to :" + colorButtons[overallsColorIndex].name);
        }
        if (clothesType == ClothesType.Shoes)
        {
            Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
            checkmarkPrefab.GetComponent<RectTransform>().SetParent(contentObject.GetChild(shoesColorIndex));
            checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;
               if(currentCharacter.shoesMaterial!=null)currentCharacter.shoesMaterial.color = colorButtons[shoesColorIndex].color;
            // Debug.Log("Set color of shoes to :" + colorButtons[shoesColorIndex].name);
        }
        if (clothesType == ClothesType.Hat)
        {
            Vector2 previousAnchoredPosition = checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition;
            checkmarkPrefab.GetComponent<RectTransform>().SetParent(contentObject.GetChild(helmetColorIndex));
            checkmarkPrefab.GetComponent<RectTransform>().anchoredPosition = previousAnchoredPosition;
            if (currentCharacter.helmetMaterial != null) currentCharacter.helmetMaterial.color = colorButtons[helmetColorIndex].color;
            // Debug.Log("Set color of hat to :" + colorButtons[helmetColorIndex].name);
        }
        // LoadColors();
    }
}
