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
    public bool isFilled;
    public bool isClaimed;
    public GameObject xpPile;
    private string claimTextText = "Claim";

    // NEW: Track which slot index this prefab currently represents (for recycling)
    public int currentIndex = -1;

    // NEW: Track tween more safely
    private Tween shakeTween;

    public void Initialize(int index, TrophyRoadManager manager, ImageGallery gallery)
    {
        currentIndex = index;
        levelProgressIndex = index; // CRITICAL: This must match the current slot
        trophyRoadManager = manager;
        imageGallery = gallery;

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(ClaimReward);

        // Refresh visual state
        if (isClaimed)
        {
            SetClaimed();
        }
        else if (isFilled)
        {
            backgroundImage.sprite = claimableSprite;
            claimButton.interactable = true;
            claimText.text = claimTextText;

            if (uIEffect != null)
            {
                uIEffect.enabled = true;


                uIEffect.SetVerticesDirty();
            }
            if (isClaimed)
                backgroundImage.color = unclaimedColor;
            else backgroundImage.color = claimedColor;

            StartTweening();
        }
        else
        {
            SetUnavailable();
        }
    }

    public void StartTweening()
    {
        KillTween();
        // Only start tween if this is claimable (not claimed, not unavailable)
        if (!isClaimed && isFilled)
        {
            // Your tween logic here - uncomment and adjust as needed
            // shakeTween = transform.DOShakeRotation(1.5f, 5f).SetLoops(-1);
        }
    }

    public void KillTween()
    {
        if (shakeTween != null)
        {
            shakeTween.Kill();
            shakeTween = null;
        }
        transform.rotation = Quaternion.identity;
    }

    public void SetClaimed()
    {
        isClaimed = true;
        backgroundImage.sprite = claimedSprite;
        claimButton.interactable = false;
        claimText.text = "";
        if (uIEffect != null) uIEffect.enabled = false;
        KillTween();
        backgroundImage.color = claimedColor;
    }

    public void SetUnavailable()
    {
        isClaimed = false;
        isFilled = false;
        KillTween();
        backgroundImage.sprite = unavailableSprite;
        claimButton.interactable = false;
        claimText.text = "";
        if (uIEffect != null) uIEffect.enabled = false;
        backgroundImage.color = unclaimedColor;
    }

    // Call this when recycling a slot to reset its state before re-initializing
    public void ResetSlot()
    {
        KillTween();
        claimButton.onClick.RemoveAllListeners();
        currentIndex = -1;
        levelProgressIndex = -1;
        isFilled = false;
        isClaimed = false;
    }

    private void SpawnCurrencyPanel()
    {
        StartCoroutine(SpawnPanel());
    }

    public IEnumerator SpawnPanel()
    {
        yield return new WaitForSeconds(0.5f);
        if (claimPanel != null && imageGallery != null)
        {
            ClaimPanel cPanel = Instantiate(claimPanel, imageGallery.transform);
        }
        yield return new WaitForSeconds(0.35f);
    }

    public void ClaimReward()
    {
        if (isClaimed) return; // Prevent double claiming

        Debug.Log("Spawning panel in GallerySlotPrefab");
        SetClaimed();
        StartCoroutine(DelayScroll());
    }

    private IEnumerator DelayScroll()
    {
        yield return new WaitForSeconds(0.2f);
        if (imageGallery != null)
        {
            imageGallery.ClaimGalleryReward(levelProgressIndex, this, 5, 50);
        }
    }
}