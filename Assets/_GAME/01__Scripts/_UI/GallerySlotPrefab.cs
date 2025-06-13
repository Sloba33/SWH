using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Coffee.UIEffects;
using DG.Tweening;
public class GallerySlotPrefab : MonoBehaviour
{
    public Image backgroundImage;
    public Button claimButton;
    public TextMeshProUGUI claimText;
    public UIEffect uIEffect;
    public TrophyRoadManager trophyRoadManager;
    public int levelProgressIndex;  
    public ImageGallery imageGallery;
    public ClaimPanel claimPanel;
    public Sprite claimableSprite, claimedSprite, unavailableSprite;
    public Color claimedColor = new();
    public Color unclaimedColor = new();
    public void Initialize(int index, TrophyRoadManager manager, ImageGallery gallery)
    {
        levelProgressIndex = index;
        trophyRoadManager = manager;
        imageGallery = gallery;
        claimButton.onClick.AddListener(ClaimReward);
        backgroundImage.sprite = claimableSprite;
     
        StartTweening();
    }
    Tween tween;
    public void StartTweening()
    {
        KillTween();
        // tween = transform.DOShakeRotation(1.5f, 5f).SetLoops(-1);
        tween.Play();
    }
    public void KillTween()
    {
        if (tween != null)
        {
            tween.Kill();  // Stop the tween
            tween = null;  // Clear the tween reference to avoid further usage
        }
        transform.rotation = Quaternion.identity;  // Reset the rotation after stopping
    }
    public void SetClaimed()
    {
        backgroundImage.sprite = claimedSprite;
        claimButton.interactable = false;
        claimText.text = "";
        uIEffect.enabled = false;
        KillTween();
        backgroundImage.color = claimedColor;
    }
    public void SetUnavailable()
    {
        KillTween();
        backgroundImage.sprite = unavailableSprite;
        claimButton.interactable = false;
        claimText.text = "";
        uIEffect.enabled = false;
        backgroundImage.color = unclaimedColor;
    }
    private void SpawnCurrencyPanel()
    {
        // rewardImage.transform.DOShakeRotation(0.65f).Play();
        StartCoroutine(SpawnPanel());
    }
    public IEnumerator SpawnPanel()
    {
        yield return new WaitForSeconds(0.5f);
        ClaimPanel cPanel = Instantiate(claimPanel, imageGallery.transform); ;


        yield return new WaitForSeconds(0.35f);

    }
    public void ClaimReward()
    {
        Debug.Log("Spawning panel in GallerySlotPrefab");
        SetClaimed();
        StartCoroutine(DelayScroll());
    }
    private IEnumerator DelayScroll()
    {
        yield return new WaitForSeconds(0.2f);
        imageGallery.ClaimGalleryReward(levelProgressIndex, this);
    }
}
