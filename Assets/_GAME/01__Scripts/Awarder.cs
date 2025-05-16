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
    [SerializeField]
    private GameObject pileOfCurrency;

    [SerializeField]
    private TextMeshProUGUI counter;

    [SerializeField]
    private Vector2[] initialPos;

    [SerializeField]
    private Quaternion[] initialRotation;

    [SerializeField]
    private int currencyAmount;
    public Transform target;

    [SerializeField]
    Sprite claimableImage,
        unclaimableImage;

    private int currentLevelProgress,
        playerLevel,
        requiredXP;

    public float xpGainDelay = 2f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            AwardCurrency(10, 10);
        }
    }

    void Start()
    {
        if (battlePassManager == null) battlePassManager = FindObjectOfType<BattlePassManager>();
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
        {
            xpToSpawn = 20;
        }
        else if (xp > 200 && xp <= Mathf.Infinity)
        {
            xpToSpawn = 30;
        }
        StartCoroutine(StartAwardCurrency(xpToSpawn, xp, xpGainDelay));


    }

    public void SetBattlePassLevelFill(float amt, string fillText, int lvl)
    {
        battlePassManager.BPFillBarGlobal.fillAmount = amt;

        battlePassManager.BPFillBarText.text = fillText;
        battlePassManager.BPLevelTextGlobal.text = lvl + "";
        battlePassManager.UpdateFillBars();
    }

    public Vector3 targetPosition;
    public RectTransform startPosition;
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
    AudioSource audioSource;
    AudioClip audioClip;

    public void LoadPlayerLevelData()
    {
        playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        if (playerLevel < 1)
            playerLevel = 1;
        currentLevelProgress = PlayerPrefs.GetInt("XP_IN_LEVEL");
        int baseXP;
        if (PlayerPrefs.GetInt("PlayerLevel") <= 1)
            baseXP = 50;
        else baseXP = 100;

        requiredXP = baseXP;


        float fillAmount = (float)currentLevelProgress / (float)requiredXP;


        fillBar.fillAmount = fillAmount;
        fillText.text = currentLevelProgress + "/" + requiredXP;

        levelText.text = playerLevel + "";
        SetBattlePassLevelFill(fillBar.fillAmount, fillText.text, playerLevel);
    }
    bool firstLevelUp;
    IEnumerator FillBar(float delay, int gainedXP, int currentLevelXP, int requiredXP)
    {
        Debug.Log(
            "Called function with the following values Delay: "
                + delay
                + " Gained XP : "
                + gainedXP
                + " Current level XP : "
                + currentLevelXP
                + " Required XP :  "
                + requiredXP
        );

        int XPToLevel = requiredXP - currentLevelXP;
        yield return new WaitForSeconds(delay);

        if (gainedXP >= XPToLevel)
        {
            int remainingXP = gainedXP - XPToLevel;


            float totalXPToFill = XPToLevel;
            float fillSpeed;
            if (!firstLevelUp)
            {
                fillSpeed = 0.5f;
                firstLevelUp = true;
            }
            else
            {
                fillSpeed = 0.08f;
            }
            float elapsedTime = 0f;

            while (elapsedTime < fillSpeed)
            {
                float increment = totalXPToFill * (Time.deltaTime / fillSpeed);
                fillBar.fillAmount += increment / requiredXP;
                fillText.text =
                    Mathf.FloorToInt(fillBar.fillAmount * requiredXP) + "/" + requiredXP;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            LevelUp();

            if (remainingXP > 0)
            {
                int baseXP;
                if (PlayerPrefs.GetInt("PlayerLevel") <= 1)
                    baseXP = 50;
                else baseXP = 100;

                XPToLevel = baseXP;
                StartCoroutine(FillBar(0.01f, remainingXP, 0, XPToLevel));
            }
        }
        else
        {

            float fillSpeed = 1f;
            float elapsedTime = 0f;
            float targetFillAmount = (currentLevelXP + gainedXP) / (float)requiredXP;

            while (elapsedTime < fillSpeed && fillBar.fillAmount < targetFillAmount)
            {
                float increment = gainedXP * (Time.deltaTime / fillSpeed);
                fillBar.fillAmount += increment / requiredXP;
                currentLevelXP = Mathf.FloorToInt(fillBar.fillAmount * requiredXP);

                fillText.text = currentLevelXP + "/" + requiredXP;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure fill is exactly at the target amount
            fillBar.fillAmount = targetFillAmount;
            fillText.text = Mathf.FloorToInt(fillBar.fillAmount * requiredXP) + "/" + requiredXP;

            // Save the current XP
            SaveCurrentLevel(Mathf.FloorToInt(fillBar.fillAmount * requiredXP));
            SetBattlePassLevelFill(
                fillBar.fillAmount,
                fillText.text,
                PlayerPrefs.GetInt("PlayerLevel")
            );
        }
        StartCoroutine(RevertTargetScale(target));
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
        int baseXP;
        if (PlayerPrefs.GetInt("PlayerLevel") <= 1)
            baseXP = 50;
        else baseXP = 100;
        requiredXP = baseXP;
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
        SetBattlePassLevelFill(fillBar.fillAmount, fillText.text, playerLevel);
        battlePassManager.SetClaimable(true);
        battlePassManager.UpdateButtons();
    }
}
