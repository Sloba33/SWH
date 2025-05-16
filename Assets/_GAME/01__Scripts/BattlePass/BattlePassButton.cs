using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using Coffee.UIEffects;

public class BattlePassButton : MonoBehaviour
{
    public GameObject lockIcon;
    public TextMeshProUGUI descriptionText;
    public Button claimButton;
    public TextMeshProUGUI claimText;
    public Image itemImage;

    public int Level { get; private set; }
    public bool IsFree { get; private set; }

    private BattlePassManager battlePassManager;
    public ClaimPanel claimPanel;
    private Sprite backgroundSprite, itemSprite;
    private Transform parent;
    public AudioSource audioSource;
    public UIShiny uiShiny;

    public void Initialize(int level, string description, Sprite sprite, string claimTextString, bool isFree, BattlePassManager manager, Sprite bgSprite, Transform parentPanel, AudioClip audioClip)
    {
        uiShiny = GetComponent<UIShiny>();
        Level = level;
        IsFree = isFree;
        descriptionText.text = "x" + $"{description}";
        itemImage.sprite = sprite;
        claimText.text = claimTextString;
        battlePassManager = manager;
        backgroundSprite = bgSprite;
        itemSprite = sprite;
        claimButton.onClick.AddListener(OnClaimButtonClick);
        parent = parentPanel;
        audioSource.clip = audioClip;

    }

    private void OnClaimButtonClick()
    {
        battlePassManager.ClaimItem(Level, IsFree, this);
        itemImage.transform.DOShakeRotation(0.65f).Play();

        StartCoroutine(SpawnPanel());


    }
    private void Start()
    {
        gameObject.name = gameObject.name + Level.ToString();
    }
    public IEnumerator SpawnPanel()
    {
        yield return new WaitForSeconds(0.5f);
        ClaimPanel cPanel = Instantiate(claimPanel, parent);
        cPanel.SetTextAndImage(descriptionText.text, itemSprite, backgroundSprite);
        yield return new WaitForSeconds(0.35f);
        audioSource.Play();
    }
    public void SetButtonState(bool isClaimable, bool isClaimed)
    {
        if (isClaimable && isClaimed)
        {
            SetButtonClaimed();
            uiShiny.enabled = false;
            // Disable claimText when the item is not claimable
        }
        else if (isClaimable && !isClaimed)
        {
            SetButtonAsUnclaimedAndAvailable();
            claimText.gameObject.SetActive(isClaimable); // Disable claimText when the item is not claimable
            uiShiny.enabled = true;
        }
        else if (!isClaimable && !isClaimed)
        {
            SetButtonAsUnclaimedAndUnavailable();
            claimText.gameObject.SetActive(isClaimable); // Disable claimText when the item is not claimable
            if (uiShiny != null) uiShiny.enabled = false;
        }
        else Debug.Log("How the f");
        // claimButton.interactable = isClaimable;
        // itemImage.color = isClaimable ? Color.white : new Color(1f, 1f, 1f, 0.5f); // Change the alpha channel when claimed

    }
    public void SetButtonNoPremium()
    {
        lockIcon.SetActive(true);
        claimButton.interactable = false;
    }
    [SerializeField] Color fadedColor;
    public void SetButtonClaimed()
    {
        // Color faded = new(1, 1, 0.65f, 0.5f);

        claimButton.GetComponent<Image>().color = fadedColor;
        itemImage.color = new Color(1, 1, 1, 0.5f);
        claimButton.interactable = false;
        claimText.gameObject.SetActive(false);
        Debug.Log("Setting as claimed");
    }
    public void SetButtonAsUnclaimedAndAvailable()
    {

        claimButton.interactable = true;
        Debug.Log("Setting as unclaimed but available");
    }
    public void SetButtonAsUnclaimedAndUnavailable()
    {

        claimButton.interactable = false;
        Debug.Log("Setting as unclaimed but unavailable");
    }
}
