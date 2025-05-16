using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrophyAwarder : MonoBehaviour
{
    public AudioSource audioSource;
    public TrophyRoadManager trophyRoadManager;
    public TextMeshProUGUI trophyCountText;
    public Image fillBar;
    public Image levelIcon;
    public TextMeshProUGUI fillText;
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
    public bool isTrophies;
    private int  trophyLevel;
    private int trophyPerLevelIncrease = 5;
    void Awake()
    {
        int trophies = PlayerPrefs.GetInt("TrophyGain");
        if (trophies == 0)
        {
            Awarder awarder = FindObjectOfType<Awarder>(true);
            awarder.xpGainDelay = 0f;
        }
    }

    void Start()
    {
        LoadPlayerLevelData();

        if (PlayerPrefs.GetInt("TrophyGain") == 0) return;
        currencyAmount = pileOfCurrency.transform.childCount; 

        initialPos = new Vector2[currencyAmount];
        initialRotation = new Quaternion[currencyAmount];

        for (int i = 0; i < pileOfCurrency.transform.childCount; i++)
        {
            initialPos[i] = pileOfCurrency.transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
            initialRotation[i] = pileOfCurrency.transform.GetChild(i).GetComponent<RectTransform>().rotation;
        }

        int trophies = PlayerPrefs.GetInt("TrophyGain");
        int trophiesToSpawn = Mathf.Min(trophies, 10);
        bool firstTrophyReward = PlayerPrefs.GetInt("FirstTrophyReward") == 0;
        if (trophies == 0) return;
        if (trophies <= 5)
        {
            trophiesToSpawn = 5;
        }
        else if (trophies > 5 && trophies <= 10)
        {
            trophiesToSpawn = 10;

        }
        else
            trophiesToSpawn = 15;

        if (firstTrophyReward)
        {

        }
        StartCoroutine(StartAwardCurrency(trophiesToSpawn, trophies));


    }
    public IEnumerator FakeFirstTimeCurrency()
    {

        while (fillBar.fillAmount < 1)
        {
            fillBar.fillAmount += 0.05f;
            yield return null;
        }
        fillBar.fillAmount = 0f;
        AudioManager.Instance.PlayUISound("trophy_levelup");
    }
    public IEnumerator StartAwardCurrency(int amt, int actualgain)
    {
        yield return new WaitForSeconds(0.2f);
        AwardCurrency(amt, actualgain);

    }
    public Vector3 targetPosition;
    public RectTransform startPosition;
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

        if (trophyRoadManager != null)
        {

            StartCoroutine(FillTheTrophyBar(trophyRoadManager.currentFillBar.fillBar.fillAmount, trophyRoadManager.currentFillBar));
            StartCoroutine(TestFill(actualGain));
        }
        else Debug.LogError("Manager is null");
    }

    public void LoadPlayerLevelData()
    {
        trophyRoadManager = FindObjectOfType<TrophyRoadManager>();
        trophyLevel = PlayerPrefs.GetInt("TrophyLevel", 1);
        if (trophyLevel < 1) trophyLevel = 1;

        fillBar.fillAmount = trophyRoadManager.currentFillBar.fillBar.fillAmount;
        trophyCountText.text = PlayerPrefs.GetInt("Trophies", 0).ToString();

    }
    // public IEnumerator FillTrophyBarNew(float previousFill, TrophyRoadFill previousTrophyRoadFill)
    // {
    //     int GainedTrophies = PlayerPrefs.GetInt("TrophyGain");
    //     List<TrophyRoadMilestone> milestones = trophyRoadManager.unclaimedMilestones;

    // }
    public IEnumerator FillTheTrophyBar(float previousFill, TrophyRoadFill previousTrophyRoadFill)
    {
        float blingCount = PlayerPrefs.GetInt("TrophyGain") / 10;
        int blings = Mathf.RoundToInt(blingCount);
        if (blings < 1) blings = 1;
        yield return new WaitForSeconds(1.15f);
        bool firstTrophyReward = PlayerPrefs.GetInt("FirstTrophyReward") == 0;
        if (firstTrophyReward)
        {
            PlayerPrefs.SetInt("FirstTrophyReward", 1);
            StartCoroutine(FakeFirstTimeCurrency());
            yield return new WaitForSeconds(0.5f);
        }
        TrophyRoadFill newTrophyRoadFill = trophyRoadManager.currentFillBar;

        if (fillBar.fillAmount < previousFill)
        {

            fillBar.fillAmount += 0.05f;
            yield return new WaitForSeconds(0.01f);
        }
        if (fillBar.fillAmount >= 0.999f || previousTrophyRoadFill != newTrophyRoadFill)
        {


            while (fillBar.fillAmount < 0.99)
            {
                fillBar.fillAmount += 0.05f;
                yield return null;

            }

            fillBar.fillAmount = 0f;

            while (blings > 0)
            {
                AudioManager.Instance.PlayUISound("trophy_levelup");
                yield return new WaitForSeconds(0.3f);
                blings--;
            }

        }
        float newFill = trophyRoadManager.currentFillBar.fillBar.fillAmount;

        if (newFill > 0)
            while (fillBar.fillAmount < newFill)
            {
                fillBar.fillAmount += 0.05f;
                yield return new WaitForSeconds(0.01f);
            }
    }
    public IEnumerator TestFill(int trophyAmount)
    {
        yield return new WaitForSeconds(1.13f);
        int currentTrophies = PlayerPrefs.GetInt("Trophies");
        PlayerPrefs.SetInt("Trophies", PlayerPrefs.GetInt("Trophies") + trophyAmount);
        float waitTime = trophyAmount - currentTrophies;
        if (waitTime > 20)
        {
            waitTime = 0.05f;
        }
        else if (waitTime > 40)
        {
            waitTime = 0.02f;
        }
        else if (waitTime < 20)
        {
            waitTime = 0.1f;
        }
        PlayerPrefs.SetInt("TrophyGain", 0);
        trophyRoadManager.GenerateEverything();
        for (int i = 0; i < trophyAmount; i++)
        {
            currentTrophies += 1;
            trophyCountText.text = currentTrophies.ToString();
            yield return new WaitForSeconds(waitTime);
        }
        target.localScale = new Vector3(1, 1, 1);

    }
    public IEnumerator FillBar(float delay, int gainedTrophies, int currentTrophyXP, int requiredTrophies)
    {
        yield return new WaitForSeconds(delay);

        int trophiesToLevel = requiredTrophies - currentTrophyXP;

        if (gainedTrophies >= trophiesToLevel)
        {
            int remainingTrophies = gainedTrophies - trophiesToLevel;
            float fillSpeed = 2.0f;
            float elapsedTime = 0f;

            while (elapsedTime < fillSpeed && fillBar.fillAmount < 1.0f)
            {
                float increment = (1.0f / fillSpeed) * Time.deltaTime;
                fillBar.fillAmount += increment;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            LevelUp();

            if (remainingTrophies > 0)
            {
                int newRequiredTrophies = 100 + (((trophyLevel * trophyPerLevelIncrease) - 20));
                StartCoroutine(FillBar(0.1f, remainingTrophies, 0, newRequiredTrophies));
            }
        }
        else
        {
            float fillSpeed = 2.0f;
            float elapsedTime = 0f;
            float targetFillAmount = (currentTrophyXP + gainedTrophies) / (float)requiredTrophies;

            while (elapsedTime < fillSpeed && fillBar.fillAmount < targetFillAmount)
            {
                float increment = (targetFillAmount - fillBar.fillAmount) * (Time.deltaTime / fillSpeed);
                fillBar.fillAmount += increment;
                fillText.text = Mathf.RoundToInt(fillBar.fillAmount * requiredTrophies) + "/" + requiredTrophies;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            fillBar.fillAmount = targetFillAmount;


            SaveCurrentLevel(Mathf.RoundToInt(fillBar.fillAmount * requiredTrophies));
        }
        StartCoroutine(RevertTargetScale(target));
        trophyCountText.text = PlayerPrefs.GetInt("Trophies", 0).ToString();
    }
    public IEnumerator RevertTargetScale(Transform target)
    {
        yield return new WaitForSeconds(0.1f);

        target.DOScale(new Vector3(1, 1, 1), 0.25f).Play();
    }

    public void SaveCurrentLevel(int currentTrophies)
    {
        PlayerPrefs.SetInt("Trophies", currentTrophies);
        PlayerPrefs.Save();
    }

    public void LevelUp()
    {
        trophyLevel++;
        PlayerPrefs.SetInt("TrophyLevel", trophyLevel);
        PlayerPrefs.SetInt("Trophies", 0);
        fillBar.fillAmount = 0f;
    }
}
