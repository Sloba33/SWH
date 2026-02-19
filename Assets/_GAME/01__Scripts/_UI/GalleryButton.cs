using Coffee.UIEffects;
using UnityEngine.UI;
using UnityEngine;

public class GalleryButton : MonoBehaviour
{
    public Sprite availableSprite, unavailableSprite;
    private Image image;
    public UIEffect uiEffect;
    void Start()
    {
        image = GetComponent<Image>();
    }
    public void SetClaimable(bool flag)
    {
        if (flag)
        {
            image.sprite = availableSprite;
            uiEffect.enabled = true;
        }
        else
        {
            image.sprite = unavailableSprite;
            uiEffect.enabled = false;
        }
    }
}
