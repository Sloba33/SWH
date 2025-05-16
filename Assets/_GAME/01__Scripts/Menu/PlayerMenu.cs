using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerMenu : MonoBehaviour
{
    public CharacterStats characterStats;
    public Transform weaponsInHand;
    public Material shirtMaterial, shirtMaterial_SecondPart, shirtMaterial_ThirdPart, shirtMaterial_Darker, overallsMaterial, overallsPocketMaterial, shoesMaterial, helmetMaterial;
    public GameObject playerStand;
    public Helmet defaultHelmet;
    public HelmetItem defaultHelmetItem;

    public Color defaultShirt, defaultShirtSecondary, defaultShirtTertiary, defaultHat, defaultOutfit, defaultOutfitSecondary, defaultOutfitPocket, defaultShoes;
    public Material faceMaterial;
    public Texture eyesOpen, eyesClosed;
    public Transform helmetParent;
    public bool hasBlinked;
    public float blinkDelay;
    public bool canBlink;


    private void Start()
    {

        Debug.Log("Playermenu loaded");
        SetColors();
        canBlink = true;
    }

    private void FixedUpdate()
    {
        StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
        if (canBlink && !hasBlinked)
        {
            hasBlinked = true;
            faceMaterial.SetTexture("_BaseMap", eyesClosed);
            yield return new WaitForSeconds(0.2f);
            faceMaterial.SetTexture("_BaseMap", eyesOpen);
            yield return new WaitForSeconds(blinkDelay);
            hasBlinked = false;
        }
    }
    public void ColorItem(Material materialToColor, Color targetColor, string colorString)
    {
        materialToColor.color = targetColor;
        SaveColor(colorString, targetColor);
        Debug.Log("Loaded default " + colorString + " color");
    }
    public void ColorShirt(Color col)
    {
        ColorItem(shirtMaterial, col, "shirt");
        if (shirtMaterial_SecondPart != null)
        {
            ColorItem(shirtMaterial_SecondPart, col, "shirtSecondary");
        }
        if (shirtMaterial_ThirdPart != null)
        {
            ColorItem(shirtMaterial_ThirdPart, col, "shirtTertiary");
        }
        if (shirtMaterial_Darker != null)
        {
            Color temp = col;
            temp = temp * (1 - 0.2f);
            ColorItem(shirtMaterial_Darker, temp, "shirtDarker");
        }
    }
    public void ColorOveralls(Color col)
    {
        ColorItem(overallsMaterial, col, "coverall");

        Color temp = col;
        temp = temp * (1 - 0.2f);
        ColorItem(overallsPocketMaterial, temp, "pocket");
    }
    public void ColorHat(Color col)
    {
        ColorItem(helmetMaterial, col, "hat");
    }
    public void ColorShoes(Color col)
    {
        ColorItem(shoesMaterial, col, "shoes");
    }
    public void SetColors()
    {
        if (!PlayerPrefs.HasKey("shirtR"))
        {
            ColorShirt(defaultShirt);
        }

        if (!PlayerPrefs.HasKey("coverallR"))
        {
            ColorOveralls(defaultOutfit);
        }

        if (!PlayerPrefs.HasKey("hatR"))
        {
            ColorHat(defaultHat);
        }

        if (!PlayerPrefs.HasKey("shoesR"))
        {
            ColorShoes(defaultShoes);
        }

    }
    public static void SaveColor(string key, Color color)
    {
        PlayerPrefs.SetFloat(key + "R", color.r);
        PlayerPrefs.SetFloat(key + "G", color.g);
        PlayerPrefs.SetFloat(key + "B", color.b);
    }
    public void SetDefaultColors()
    {
        if (shirtMaterial != null) shirtMaterial.color = defaultShirt;
        if (helmetMaterial != null) helmetMaterial.color = defaultHat;
        if (overallsMaterial != null) overallsMaterial.color = defaultOutfit;
        if (overallsPocketMaterial != null) overallsPocketMaterial.color = defaultOutfitPocket;
        if (shoesMaterial != null) shoesMaterial.color = defaultShoes;
    }
}
