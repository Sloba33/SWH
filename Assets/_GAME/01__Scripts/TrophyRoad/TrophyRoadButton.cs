using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Coffee.UIEffects;
using UnityEngine.TextCore.Text;
public class TrophyRoadButton : MonoBehaviour
{
    public int TrophyRequirement { get; private set; }
    public Button button;
    public TextMeshProUGUI amountText;
    public Image rewardImage;
    public GameObject chestObject;
    public TrophyRoadManager manager;
    private Sprite backgroundSprite;
    public ClaimPanel claimPanel;
    private Transform parent;
    public TrophyRoadData trophyRoadData;
    public Sprite characterPortraitSprite;
    [SerializeField] AudioSource audioSource;
    public UIShiny uiShiny;
    [Header("Tween Settings")]
    [SerializeField] float speed;
    [SerializeField] float scale;
    [SerializeField] float amount;
    TrophyRewardType trophyRoadRewardType;
    public CharacterTokenPanel characterTokenPanel;

    public void Initialize(int trophyRequirement, string amount, string description, Sprite rewardSprite, Sprite bgSprite, TrophyRoadManager manager, Transform parentPanel, bool character, TrophyRewardType trophyReward, TrophyRoadData trophyRoadData, AudioClip audioclip)
    {
        Debug.Log("REWARD TYPE :" + trophyReward);
        CustomScrollTrophyRoad cstr = FindObjectOfType<CustomScrollTrophyRoad>(true);
        button.onClick.RemoveAllListeners();
        TrophyRequirement = trophyRequirement;
        if (character)
        {
            amountText.text = description;
        }
        else amountText.text = "x" + amount.ToString();
        uiShiny = GetComponent<UIShiny>();
        rewardImage.sprite = rewardSprite;
        this.manager = manager;
        backgroundSprite = bgSprite;

        // button.onClick.AddListener(() => TrophyRoadFlagChanger(true));
        button.onClick.AddListener(() => ShowReward(trophyRoadData));
        if (trophyReward == TrophyRewardType.Character_Token) button.onClick.AddListener(() => manager.ClaimReward(trophyRequirement, 4f));
        else button.onClick.AddListener(() => manager.ClaimReward(trophyRequirement, 1f));
        trophyRoadRewardType = trophyReward;
        this.trophyRoadData = trophyRoadData;
        parent = parentPanel;
        gameObject.name = TrophyRequirement.ToString();
        // button.onClick.AddListener(() => ShowReward(trophyRoadData));
        if (audioclip == null) Debug.Log("Audio CLIP IS NULL");
        else
        {
            Debug.Log("Audio clip name " + audioclip.name);
            audioSource.clip = audioclip;
        }


    }

    private void TrophyRoadFlagChanger(bool flag)
    {
        CharacterPickerManager cpm = FindObjectOfType<CharacterPickerManager>(true);
        if (cpm != null)
        {

            cpm.TrophyRoadTempFlag = flag;
            Debug.Log("Set TropgyRoadTempFlag to " + flag);
        }
        else Debug.Log("CPM NULL");
    }
    public CharacterTokenManager characterTokenManager;
    private void ShowReward(TrophyRoadData trophyRoadData)
    {
        Debug.Log("Showing +Reward");
        switch (trophyRoadRewardType)
        {
            case TrophyRewardType.Coins_Small:
            case TrophyRewardType.Coins_Medium:
            case TrophyRewardType.Coins_Large:
            case TrophyRewardType.Gems_Small:
            case TrophyRewardType.Gems_Medium:
            case TrophyRewardType.Gems_Large:
            case TrophyRewardType.Money_Small:
            case TrophyRewardType.Money_Medium:
            case TrophyRewardType.Money_Large:
            case TrophyRewardType.Weapon_Pickaxe:
            case TrophyRewardType.Weapon_Axe:
            case TrophyRewardType.Weapon_Bat:
            case TrophyRewardType.Helmet_Bike:
            case TrophyRewardType.Helmet_Rugby:
                SpawnCurrencyPanel();
                break;

            case TrophyRewardType.Chest_Currency:
                SpawnCurrencyChestPanel();
                break;

            case TrophyRewardType.Character_Token:
                HandleCharacterTokenReward();
                break;

            default:
                Debug.LogError($"Unhandled reward type: {trophyRoadRewardType}");
                break;
        }

    }

    private void HandleCharacterTokenReward()
    {
        characterTokenManager = FindObjectOfType<CharacterTokenManager>(true);

        if (characterTokenManager.characterStats == null)
        {
            Debug.LogError("characterStats is not assigned in CharacterTokenManager.");
            return;
        }

        // if (characterTokenManager.previousCharacterStats == null)
        // {
        //     Debug.LogError("previousCharacterStats is not assigned in CharacterTokenManager.");
        //     // return;
        // }
        TrophyRoadFlagChanger(true);
        if (!characterTokenManager.twice)
        {
            SpawnTokenPanels(characterTokenManager.characterStats, null);
            Debug.Log("Spawning once");
        }
        else
        {
            SpawnTokenPanels(characterTokenManager.previousCharacterStats, characterTokenManager.characterStats);
            Debug.Log("Spawning twice");
        }
        //   TrophyRoadFlagChanger(false);
    }
    private void Start()
    {
        button.onClick.AddListener(KillTween);
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
    private void SpawnCurrencyPanel()
    {
        rewardImage.transform.DOShakeRotation(0.65f).Play();
        StartCoroutine(SpawnPanel());
    }
    private void SpawnCurrencyChestPanel()
    {
        StartCoroutine(SpawnChestPanel());
    }

    private void SpawnTokenPanels(CharacterStats firstCharacterStats, CharacterStats secondCharacterStats)
    {
        Debug.Log("Spawning token panels");
        StartCoroutine(SpawnTokenPanelCoroutineSingle(firstCharacterStats));
    }

    public IEnumerator SpawnTokenPanelCoroutineSingle(CharacterStats characterStats)
    {
        int currentTokens = PlayerPrefs.GetInt("CharacterTokens", 0)+100;
        Debug.Log("Current Tokens : " + currentTokens);
        int neededTokens = characterStats.tokensRequired;
        Debug.Log("Needed Tokens : " + neededTokens);
        bool full = currentTokens >= neededTokens;
        Debug.Log("Full : " + full);
        yield return new WaitForSeconds(0.1f);

        CharacterTokenPanel ctp = Instantiate(characterTokenPanel, manager.tokenPanelSpawnParent);
        ctp.SetPortrait(characterStats);
        RectTransform ctpRect = ctp.GetComponent<RectTransform>();

        // Reset local rotation and scale to ensure it's aligned properly
        ctpRect.localRotation = Quaternion.identity;
        ctpRect.localScale = Vector3.one;
        ctpRect.DOLocalMove(new Vector3(ctpRect.localPosition.x, ctpRect.localPosition.y - 285f, ctpRect.localPosition.z), 0.9f).Play();

        yield return new WaitForSeconds(1f);
        // Generate tokens after waiting
        ctp.GenerateTokens(transform, characterStats, full, 10, this);
    }
    public IEnumerator SpawnTokenPanelCoroutine(CharacterStats firstCharacterStats, CharacterStats secondCharacterStats)
    {
        yield return new WaitForSeconds(0.25f);

        CharacterTokenPanel ctp = Instantiate(characterTokenPanel, manager.tokenPanelSpawnParent);
        CharacterTokenPanel ctp2;
        ctp.SetPortrait(firstCharacterStats);
        // Get the RectTransform component
        RectTransform ctpRect = ctp.GetComponent<RectTransform>();

        // Reset local rotation and scale to ensure it's aligned properly
        ctpRect.localRotation = Quaternion.identity;
        ctpRect.localScale = Vector3.one;

        // Set the localPosition to (0, 165, 0) with respect to its top-center anchor
        // ctpRect.localPosition = new Vector3(0, 165, 0);
        ctpRect.DOLocalMove(new Vector3(ctpRect.localPosition.x, ctpRect.localPosition.y - 285f, ctpRect.localPosition.z), 0.9f).Play();

        yield return new WaitForSeconds(1f);
        bool full = false;
        if (secondCharacterStats != null || characterTokenManager.isEqual) full = true;
        // Generate tokens after waiting
        ctp.GenerateTokens(transform, firstCharacterStats, full, 10, this);

        if (secondCharacterStats != null)
        {

            ctp2 = Instantiate(characterTokenPanel, manager.tokenPanelSpawnParent);
            ctp2.SetPortrait(secondCharacterStats);
            ctp2.transform.SetSiblingIndex(ctp.transform.GetSiblingIndex());
            // Get the RectTransform component
            RectTransform ctpRect2 = ctp2.GetComponent<RectTransform>();

            // Reset local rotation and scale to ensure it's aligned properly
            ctpRect2.localRotation = Quaternion.identity;
            ctpRect2.localScale = Vector3.one;

            // Set the localPosition to (0, 165, 0) with respect to its top-center anchor
            ctpRect2.localPosition = ctpRect.localPosition;
            ctpRect2.DOLocalMove(new Vector3(ctpRect2.localPosition.x, ctpRect2.localPosition.y - 180f, ctpRect2.localPosition.z), 0.9f).Play();
            yield return new WaitForSeconds(1f);
            ctp2.GenerateTokens(transform, secondCharacterStats, false, 3, this);
        }
        yield return new WaitForSeconds(1f);

    }

    public IEnumerator SpawnPanel()
    {
        yield return new WaitForSeconds(0.5f);
        ClaimPanel cPanel = Instantiate(claimPanel, parent);
        if (trophyRoadRewardType == TrophyRewardType.Character_Token)
        {
            Debug.Log("Spawning Characters CPanel instead");
            cPanel.SetTextAndImageCharacter(amountText.text, rewardImage.sprite, backgroundSprite);
        }
        else
            cPanel.SetTextAndImage(amountText.text, rewardImage.sprite, backgroundSprite);

        yield return new WaitForSeconds(0.35f);
        audioSource.Play();
    }
    public IEnumerator SpawnCharacterUnlockPanel(bool second)
    {
        yield return new WaitForSeconds(0.5f);
        ClaimPanel cPanel = Instantiate(claimPanel, parent);
        cPanel.SetTextAndImage(amountText.text, rewardImage.sprite, backgroundSprite);
        if (second)

            yield return new WaitForSeconds(0.35f);
        audioSource.Play();
    }
    public IEnumerator SpawnChestPanel()
    {

        yield return new WaitForSeconds(0.5f);
        ClaimPanel cPanel = Instantiate(claimPanel, parent);
        cPanel.SetTextAndImage("", trophyRoadData.chestSprite, trophyRoadData.backgroundBlue);
        cPanel.chestObject.SetActive(true);
        cPanel.itemImage.gameObject.SetActive(false);

        Destroy(cPanel.gameObject, 3.05f);
        yield return new WaitForSeconds(1.8f);
        audioSource.Play();
        yield return new WaitForSeconds(1.2f);
        ClaimPanel firstClaimPanel = Instantiate(claimPanel, parent);
        firstClaimPanel.SetTextAndImage("x300", trophyRoadData.coinSprite_Large, trophyRoadData.backgroundYellow);
        audioSource.clip = trophyRoadData.audioClipGold;
        firstClaimPanel.button.onClick.AddListener(SpawnGemPanel);
        yield return new WaitForSeconds(0.2f);
        audioSource.Play();

    }
    public void SpawnGemPanel()
    {
        StartCoroutine(SpawnGemPanelCoroutine());
    }
    public IEnumerator SpawnGemPanelCoroutine()
    {
        ClaimPanel cPanel = Instantiate(claimPanel, parent);
        cPanel.SetTextAndImage("x50", trophyRoadData.gemSprite_Medium, trophyRoadData.backgroundPink);
        audioSource.clip = trophyRoadData.audioClipGems;
        cPanel.button.onClick.AddListener(SpawnMoneyPanel);
        yield return new WaitForSeconds(0.2f);
        audioSource.Play();

    }
    public void SpawnMoneyPanel()
    {
        StartCoroutine(SpawnMoneyPanelCoroutine());
    }
    public IEnumerator SpawnMoneyPanelCoroutine()
    {
        ClaimPanel thirdClaimPanel = Instantiate(claimPanel, parent);
        thirdClaimPanel.SetTextAndImage("x150", trophyRoadData.moneySprite_Large, trophyRoadData.backgroundGreen);
        audioSource.clip = trophyRoadData.audioClipGold;
        yield return new WaitForSeconds(0.2f);
        audioSource.Play();

    }
    public void SetButtonState(bool isClaimable, bool isClaimed)
    {
        if (isClaimable && isClaimed)
        {
            SetButtonClaimed();
            uiShiny.enabled = false;
        }
        else if (isClaimable && !isClaimed)
        {
            SetButtonAsUnclaimedAndAvailable();
            uiShiny.enabled = true;
        }
        else if (!isClaimable && !isClaimed)
        {
            SetButtonAsUnclaimedAndUnavailable();
            uiShiny.enabled = false;
        }
        // else Debug.LogError("Unexpected state: Check your claim logic!");

    }
    private Tween tween;
    [SerializeField] Color fadedColor;
    public void SetButtonClaimed()
    {
        button.GetComponent<Image>().color = fadedColor;
        Color faded = new(1, 1, 1, 0.5f);
        rewardImage.color = faded;
        button.interactable = false;
        // claimText.gameObject.SetActive(false);
        Debug.Log("Setting as claimed :" + TrophyRequirement);
        KillTween();


    }
    public void SetButtonAsUnclaimedAndAvailable()
    {

        button.interactable = true;
        Debug.Log("Setting as unclaimed but available: " + TrophyRequirement);

        // Ensure any old tween is killed before starting a new one
        KillTween();

        // Start the wiggle effect only if not already claimed
        tween = transform.DOShakeRotation(1.5f, 5f).SetLoops(-1);
        tween.Play();

    }
    public void SetButtonAsUnclaimedAndUnavailable()
    {

        button.interactable = false;
        Debug.Log("Setting as unclaimed but unavailable " + TrophyRequirement);
        KillTween();
    }

}
