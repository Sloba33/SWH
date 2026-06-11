using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Awarder : MonoBehaviour
{
    public BattlePassManager battlePassManager;
    public TrophyAwarder trophyAwarder;

    public Image fillBar;
    public Image levelIcon;

    public TextMeshProUGUI fillText;
    public TextMeshProUGUI levelText;

    public AudioSource audioSourceXP;

    [SerializeField] private GameObject pileOfCurrency;
    [SerializeField] private TextMeshProUGUI counter;

    [SerializeField] private Vector2[] initialPos;
    [SerializeField] private Quaternion[] initialRotation;

    [SerializeField] private int currencyAmount;

    public Transform target;

    [SerializeField] Sprite claimableImage, unclaimableImage;

    private int currentLevelProgress;
    private int playerLevel;
    private int requiredXP;

    public float xpGainDelay = 2f;

    public Vector3 targetPosition;
    public RectTransform startPosition;

    private Canvas xpCanvas;
    private Coroutine hideSortingCoroutine;

    AudioSource audioSource;
    AudioClip audioClip;

    private int pendingXp = 0;
    private bool isProcessingXp = false;
    private bool isXpFillAnimating = false;

    // NEW
    private int activeBubbleAnimations = 0;

    float xpFillDelay = 0.01f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            AwardCurrency(10, 10);
        }
    }

    void Start()
    {
        xpCanvas = battlePassManager.xpFillBar.GetComponent<Canvas>();

        if (battlePassManager == null)
            battlePassManager = FindObjectOfType<BattlePassManager>();

        audioSource = GetComponent<AudioSource>();
        audioClip = audioSource.clip;

        trophyAwarder = FindObjectOfType<TrophyAwarder>();

        LoadPlayerLevelData();

        if (PlayerPrefs.GetInt("XPGain") == 0)
            return;

        currencyAmount = pileOfCurrency.transform.childCount;

        initialPos = new Vector2[currencyAmount];
        initialRotation = new Quaternion[currencyAmount];

        for (int i = 0; i < pileOfCurrency.transform.childCount; i++)
        {
            initialPos[i] = pileOfCurrency
                .transform.GetChild(i)
                .GetComponent<RectTransform>()
                .anchoredPosition;

            initialRotation[i] = pileOfCurrency
                .transform.GetChild(i)
                .GetComponent<RectTransform>()
                .rotation;
        }

        int xp = PlayerPrefs.GetInt("XPGain");

        int xpToSpawn = 0;

        if (xp <= 0)
            return;

        if (xp <= 100)
            xpToSpawn = 10;
        else if (xp > 100 && xp <= 200)
            xpToSpawn = 20;
        else
            xpToSpawn = 30;

        // Don't use AwardCurrency - use AddXp directly after a delay
        StartCoroutine(DelayedXpOnStart(xpToSpawn, xp, xpGainDelay));
    }
    private IEnumerator DelayedXpOnStart(int bubbleCount, int actualXp, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Animate bubbles first
        for (int i = 0; i < bubbleCount; i++)
        {
            targetPosition = pileOfCurrency
                .transform.GetChild(i)
                .GetComponent<RectTransform>()
                .position;

            pileOfCurrency.transform.GetChild(i).GetComponent<RectTransform>().position =
                startPosition.position;

            pileOfCurrency.transform.GetChild(i).gameObject.SetActive(true);

            pileOfCurrency.transform.GetChild(i).DOMove(targetPosition, 0.4f).Play();
        }

        var bubbleDelay = 0f;

        for (int i = 0; i < bubbleCount; i++)
        {
            pileOfCurrency
                .transform.GetChild(i)
                .DOScale(1.2f, 0.3f)
                .SetDelay(bubbleDelay)
                .SetEase(Ease.OutBack)
                .Play();

            pileOfCurrency
                .transform.GetChild(i)
                .GetComponent<RectTransform>()
                .DOMove(target.transform.position, 0.8f)
                .SetDelay(bubbleDelay + 0.5f)
                .SetEase(Ease.InBack)
                .Play();

            pileOfCurrency
                .transform.GetChild(i)
                .DORotate(Vector3.zero, 0.5f)
                .SetDelay(bubbleDelay + 0.5f)
                .SetEase(Ease.Flash)
                .Play();

            pileOfCurrency
                .transform.GetChild(i)
                .DOScale(0f, 0.2f)
                .SetDelay(bubbleDelay + 1.30f)
                .SetEase(Ease.OutBack)
                .Play();

            bubbleDelay += 0.05f;

            counter
                .transform.parent.GetChild(0)
                .transform.DOScale(1.1f, 0.1f)
                .SetLoops(10, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(1.2f)
                .Play();

            target
                .transform.DOScale(1.4f, 0.05f)
                .SetLoops(10, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(1.3f)
                .Play();
        }

        // Now add XP after bubbles
        yield return new WaitForSeconds(1.15f);
        AddXp(actualXp);

        PlayerPrefs.SetInt("XPGain", 0);
        Debug.Log("Start XP awarded: " + actualXp);
    }
    public void SetBattlePassLevelFill(float amt, string fillText, int lvl)
    {
        battlePassManager.BPFillBarGlobal.fillAmount = amt;
        battlePassManager.BPFillBarText.text = fillText;
        battlePassManager.BPLevelTextGlobal.text = lvl + "";

        battlePassManager.UpdateFillBars();
    }

    public IEnumerator StartAwardCurrency(int amt, int actualGain, float delay)
    {
        yield return new WaitForSeconds(delay);

        AwardCurrency(amt, actualGain);

        PlayerPrefs.SetInt("XPGain", 0);

        Debug.Log("delay :" + delay);
    }

    public void AwardCurrency(int amt, int actualGain)
    {
        for (int i = 0; i < amt; i++)
        {
            targetPosition = pileOfCurrency
                .transform.GetChild(i)
                .GetComponent<RectTransform>()
                .position;

            pileOfCurrency.transform.GetChild(i).GetComponent<RectTransform>().position =
                startPosition.position;

            pileOfCurrency.transform.GetChild(i).gameObject.SetActive(true);

            pileOfCurrency.transform.GetChild(i).DOMove(targetPosition, 0.4f).Play();
        }

        var delay = 0f;

        for (int i = 0; i < amt; i++)
        {
            pileOfCurrency
                .transform.GetChild(i)
                .DOScale(1.2f, 0.3f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .Play();

            pileOfCurrency
                .transform.GetChild(i)
                .GetComponent<RectTransform>()
                .DOMove(target.transform.position, 0.8f)
                .SetDelay(delay + 0.5f)
                .SetEase(Ease.InBack)
                .Play();

            pileOfCurrency
                .transform.GetChild(i)
                .DORotate(Vector3.zero, 0.5f)
                .SetDelay(delay + 0.5f)
                .SetEase(Ease.Flash)
                .Play();

            pileOfCurrency
                .transform.GetChild(i)
                .DOScale(0f, 0.2f)
                .SetDelay(delay + 1.30f)
                .SetEase(Ease.OutBack)
                .Play();

            delay += 0.05f;

            counter
                .transform.parent.GetChild(0)
                .transform.DOScale(1.1f, 0.1f)
                .SetLoops(10, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(1.2f)
                .Play();

            target
                .transform.DOScale(1.4f, 0.05f)
                .SetLoops(10, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(1.3f)
                .Play();
        }

        StartCoroutine(FillBar(1f, actualGain, currentLevelProgress, requiredXP));
    }

    private IEnumerator XpBarOverrideSortingToggle(bool flag, float delay)
    {
        yield return new WaitForSeconds(delay);

        battlePassManager.xpFillBar.GetComponent<Canvas>().overrideSorting = flag;
    }

    public void AwardCurrencyManual(
        int actualGain,
        GameObject targetPosition,
        GameObject xpPile,
        RectTransform startPos
    )
    {
        activeBubbleAnimations++;

        if (hideSortingCoroutine != null)
        {
            StopCoroutine(hideSortingCoroutine);
            hideSortingCoroutine = null;
        }

        xpCanvas.overrideSorting = true;

        Canvas bubbleCanvas = xpPile.GetComponent<Canvas>();

        if (bubbleCanvas == null)
        {
            bubbleCanvas = xpPile.AddComponent<Canvas>();
            bubbleCanvas.overrideSorting = true;
        }

        bubbleCanvas.sortingOrder = 100;

        CanvasGroup canvasGroup = xpPile.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = xpPile.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;

        xpPile.transform.SetAsLastSibling();

        bool firstBubbleArrived = false;

        for (int i = 0; i < 5; i++)
        {
            Vector3 tarPos = targetPosition
                .transform.GetChild(i)
                .GetComponent<RectTransform>()
                .position;

            xpPile.transform.GetChild(i).GetComponent<RectTransform>().position =
                startPos.position;

            xpPile.transform.GetChild(i).gameObject.SetActive(true);

            xpPile.transform.GetChild(i).DOMove(tarPos, 0.4f).Play();
        }

        var delay = 0f;

        for (int i = 0; i < 5; i++)
        {
            int bubbleIndex = i;

            xpPile
                .transform.GetChild(i)
                .DOScale(1.2f, 0.3f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .Play();

            Tween moveTween = xpPile
                .transform.GetChild(i)
                .GetComponent<RectTransform>()
                .DOMove(target.transform.position, 0.8f)
                .SetDelay(delay + 0.5f)
                .SetEase(Ease.InBack)
                .Play();

            moveTween.OnComplete(() =>
            {
                if (bubbleIndex == 0 && !firstBubbleArrived)
                {
                    firstBubbleArrived = true;
                    StartCoroutine(StartXpFillAfterBubble(actualGain));
                }

                activeBubbleAnimations--;

                TryDisableXpCanvas();
            });

            xpPile
                .transform.GetChild(i)
                .DORotate(Vector3.zero, 0.5f)
                .SetDelay(delay + 0.5f)
                .SetEase(Ease.Flash)
                .Play();

            xpPile
                .transform.GetChild(i)
                .DOScale(0f, 0.2f)
                .SetDelay(delay + 1.30f)
                .SetEase(Ease.OutBack)
                .Play();

            delay += 0.05f;

            counter
                .transform.parent.GetChild(0)
                .transform.DOScale(1.1f, 0.1f)
                .SetLoops(10, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(1.2f)
                .Play();

            target
                .transform.DOScale(1.4f, 0.05f)
                .SetLoops(10, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(1.3f)
                .Play();
        }
    }

    private IEnumerator StartXpFillAfterBubble(int actualGain)
    {
        yield return new WaitForSeconds(0.1f);

        AddXp(actualGain);
    }
    [NaughtyAttributes.Button("Test Award XP")]
    public void TestAwardXp()
    {
        AddXp(50);
        Debug.Log("Test: Awarded 50 XP");
    }
    public void AddXp(int amount)
    {
        pendingXp += amount;

        if (!isProcessingXp)
        {
            StartCoroutine(ProcessXp());
        }
    }

    private IEnumerator ProcessXp()
    {
        isProcessingXp = true;
        isXpFillAnimating = true;

        while (pendingXp > 0)
        {
            int xpToApply = pendingXp;
            pendingXp = 0;

            yield return StartCoroutine(FillBarInternal(xpToApply));
        }

        isProcessingXp = false;
        isXpFillAnimating = false;

        TryDisableXpCanvas();
    }

    public void LoadPlayerLevelData()
    {
        playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        if (playerLevel < 1)
            playerLevel = 1;

        currentLevelProgress = PlayerPrefs.GetInt("XP_IN_LEVEL", 0);

        int baseXP = GetBaseXPForLevel(playerLevel);

        requiredXP = baseXP;

        float fillAmount = (float)currentLevelProgress / (float)requiredXP;

        fillBar.fillAmount = fillAmount;

        fillText.text = currentLevelProgress + "/" + requiredXP;

        levelText.text = playerLevel + "";

        SetBattlePassLevelFill(fillBar.fillAmount, fillText.text, playerLevel);
    }

    private int GetBaseXPForLevel(int level)
    {
        return level <= 1 ? 50 : 100;
    }

    IEnumerator FillBar(float delay, int gainedXP, int currentLevelXP, int requiredXP)
    {
        yield return null;
        AddXp(gainedXP);
    }

    private IEnumerator FillBarInternal(int gainedXP)
    {
        yield return new WaitForSeconds(xpFillDelay);

        int remainingXP = gainedXP;

        while (remainingXP > 0)
        {
            int xpToLevel = requiredXP - currentLevelProgress;

            int xpThisStep = Mathf.Min(remainingXP, xpToLevel);

            float startFill = (float)currentLevelProgress / requiredXP;

            float endFill = (float)(currentLevelProgress + xpThisStep) / requiredXP;

            bool isFirstStepOfThisClaim = (remainingXP == gainedXP);

            float duration = isFirstStepOfThisClaim ? 0.5f : 0.08f;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                float fill = Mathf.Lerp(startFill, endFill, t);

                fillBar.fillAmount = fill;

                int displayedXP = Mathf.FloorToInt(fill * requiredXP);

                fillText.text = displayedXP + "/" + requiredXP;

                yield return null;
            }

            fillBar.fillAmount = endFill;

            currentLevelProgress += xpThisStep;

            remainingXP -= xpThisStep;

            SaveCurrentLevel(currentLevelProgress);

            SetBattlePassLevelFill(
                fillBar.fillAmount,
                fillText.text,
                playerLevel
            );

            if (currentLevelProgress >= requiredXP)
            {
                LevelUp();
            }
        }

        StartCoroutine(RevertTargetScale(target));
    }

    private void TryDisableXpCanvas()
    {
        if (activeBubbleAnimations <= 0 && !isXpFillAnimating)
        {
            activeBubbleAnimations = 0;

            if (hideSortingCoroutine != null)
            {
                StopCoroutine(hideSortingCoroutine);
            }

            hideSortingCoroutine = StartCoroutine(DisableSortingWithDelay(0.25f));
        }
    }

    private IEnumerator DisableSortingWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (activeBubbleAnimations == 0 && !isXpFillAnimating)
        {
            xpCanvas.overrideSorting = false;
        }

        hideSortingCoroutine = null;
    }

    public IEnumerator RevertTargetScale(Transform target)
    {
        yield return new WaitForSeconds(0.1f);

        target.DOScale(new Vector3(1, 1, 1), 0.25f).Play();
    }

    public void SaveCurrentLevel(int currentXP)
    {
        PlayerPrefs.SetInt("XP_IN_LEVEL", currentXP);
        PlayerPrefs.Save();
    }

    public void LevelUp()
    {
        playerLevel++;

        PlayerPrefs.SetInt("PlayerLevel", playerLevel);

        levelText.text = playerLevel + "";

        currentLevelProgress = 0;

        PlayerPrefs.SetInt("XP_IN_LEVEL", 0);

        fillBar.fillAmount = 0f;

        requiredXP = GetBaseXPForLevel(playerLevel);

        fillText.text = currentLevelProgress + "/" + requiredXP;

        AudioManager.Instance.PlayUISound("battlepass_levelup");

        if (battlePassManager != null)
        {
            battlePassManager.CheckForUnlockedItems();
            battlePassManager.UpdateUnclaimedCounter();
        }
        else
        {
            Debug.LogWarning("BattlePassManager is not assigned.");
        }

        SetBattlePassLevelFill(
            fillBar.fillAmount,
            fillText.text,
            playerLevel
        );

        battlePassManager.SetClaimable(true);
        battlePassManager.UpdateButtons();
    }
}