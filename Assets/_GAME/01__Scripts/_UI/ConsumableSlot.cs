using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsumableSlot : MonoBehaviour
{
    public Image img;
    public Bomb bombPrefab;
    public CollectibleItem ci;
    public int counter;
    public Button btn;
    public TextMeshProUGUI counterText;
    public BombCollectible.BombType slotType;
    public Image bombBackgroundImage;
    private void Start()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();
    }
    public void SetConsumable(CollectibleItem collectibleItem)
    {
        ci = collectibleItem;
        // Debug.Log("CI : " + ci.name);
        if (collectibleItem.isConsumable)
        {
            // Debug.Log("Item is consumable");
            slotType = collectibleItem.GetComponent<BombCollectible>().bombType;
        }
        counter++;
        counterText.text = counter + "";
        // Debug.Log("CollectibleItem " + collectibleItem.name + "" + slotType);
        bombPrefab = collectibleItem.GetComponent<BombCollectible>().bombPrefab;
        // Debug.Log("Bomb prefab is " + bombPrefab.name);
        img.sprite = collectibleItem.collectibleSprite;
    }

    public void ClearSlot()
    {
    
        bombPrefab = null;
        ci=null;
        counter = 0;
        counterText.text = ""+0;
        slotType = BombCollectible.BombType.None;
        img.sprite = null;
        gameObject.SetActive(false);
    }
}
