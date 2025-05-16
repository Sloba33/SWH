using System.Collections.Generic;
using UnityEngine;

public class PlayerColorsGlobal : MonoBehaviour
{
  
    public List<GameObject> playerPrefabs = new();
    public GameObject playerPrefab;
    public static PlayerColorsGlobal Instance;

    public static Color current_shirtColor, current_shirtSleeveColor, current_bodyColor, current_bodyPocketColor, current_hatColor, current_shoesColor;
    public Color CurrentShirtColor, CurrentShirtSleeveColor, CurrentBodyColor, CurrentBodyPocketColor, CurrentHatColor, CurrentShoesColor;

   
    // public void UpdateCurrentColors(Color color, string clothesType)
    // {
    //     if (clothesType == "Shirt")
    //     {
    //         current_shirtColor = color;
    //         current_shirtSleeveColor = color;
    //         CurrentShirtColor = current_shirtColor;
    //         CurrentShirtSleeveColor = current_shirtSleeveColor;
    //     }
    //     else if (clothesType == "Coveralls")
    //     {
    //         current_bodyColor = color;
    //         current_bodyPocketColor = color;
    //         CurrentBodyColor = current_bodyColor;
    //         CurrentBodyPocketColor = current_bodyPocketColor;
    //     }
    //     else if (clothesType == "Hat")
    //     {

    //         current_hatColor = color;
    //         CurrentHatColor = current_hatColor;
    //     }
    //     else if (clothesType == "Shoes")
    //     {
    //         current_shoesColor = color;
    //         CurrentShoesColor = current_shoesColor;
    //     }
    // }

    // private void Awake()
    // {
    //     // start of new code
    //     if (Instance != null)
    //     {
    //         Destroy(gameObject);
    //         return;
    //     }
    //     // end of new code

    //     Instance = this;
    //     // DontDestroyOnLoad(gameObject);
    // }
    // public void SetGameplayPrefab(int index)
    // {
    //     Debug.Log("Setting player prefab to index: " + index + " which is :" + playerPrefabs[index]);
    //     playerPrefab = playerPrefabs[index];
    //     // networkManager.AddNetworkPrefab(playerPrefab);
    // }
}
