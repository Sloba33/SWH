using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Coffee.UIExtensions;


public class WinScreen : MonoBehaviour
{
    public WinStarAnimator winStarAnimator;
    public bool sceneOverride;
    public string sceneOverrideName = "01_MainMenu";
    public TextMeshProUGUI levelCompleteText;
    public TextMeshProUGUI currentTime, totalTime;
    public bool MP;
    public GameObject rewardsPanel;
    public GameObject panelXP, panelTrophies;
    public GameObject proceedButton;
    public GameObject playerEmoteObject;
    public LevelGoal levelGoal;
    // Start is called before the first frame update
    public Button nextLevelButton, mainMenuButton;
    public TextMeshProUGUI XPText, TrophyText;
    public AudioSource audioSourceXP, audioSourceTrophies;
    public AudioClip audioClip;
    GoalSetter gs;
    public GameObject questPanelPrefab;
    public GameObject parentPanel;
    public Transform emotePosition;
    public LevelProgress levelProgress;
    public Transform playerGameObject;
    public UIParticle uIParticle;
    public GameObject endChapterScreen;


    private void Start()
    {

        GameManager.Instance.FreezeObstacles();
        AudioManager.Instance.BGMVolume = 0.1f;
        playerEmoteObject = FindObjectOfType<Player>().gameObject;
        if (playerEmoteObject != null)
        {
            playerEmoteObject.GetComponent<Animator>().SetBool("Idle", false);
            Player p = FindObjectOfType<Player>();
            p.EndScreenCamera.SetActive(true);
            playerEmoteObject.GetComponent<Animator>().Play("Victory_2");

        }
        if (levelGoal == null)
            levelGoal = FindFirstObjectByType<LevelGoal>();
        if (levelGoal.DualLevel)
        {
            gs = FindObjectOfType<GoalSetter>();
            if (gs != null) gs.gameObject.SetActive(false);
        }

        SceneLoader sceneLoader = FindObjectOfType<SceneLoader>();
        nextLevelButton.onClick.AddListener(() =>
              {

                  if (GameManager.Instance != null)
                  {
                      Destroy(GameManager.Instance.gameObject);
                  }

                  if (!sceneOverride)
                      sceneLoader.LoadSpecificScene(sceneLoader.nextLevelIndex);
                  else sceneLoader.LoadSceneFile(sceneOverrideName);



              });
        mainMenuButton.onClick.AddListener(() =>
       {
           //    NetworkManager.Singleton.Shutdown();
           //    if (NetworkManager.Singleton != null)
           //    {
           //        Destroy(NetworkManager.Singleton.gameObject);
           //    }
           if (GameManager.Instance != null)
           {
               Destroy(GameManager.Instance.gameObject);
           }
           Debug.Log("added");
           sceneLoader.LoadMainMenu();
       });
        if (proceedButton != null)
        {
            if (!GameManager.Instance.isFinalChapterLevel)
                proceedButton.GetComponent<Button>().onClick.AddListener(() =>
                             {

                                 if (GameManager.Instance != null)
                                 {
                                     Destroy(GameManager.Instance.gameObject);
                                 }

                                 sceneLoader.LoadMainMenu();
                             });
            else
            {
                proceedButton.GetComponent<Button>().onClick.AddListener(() =>
                            {

                                if (GameManager.Instance != null)
                                {
                                    Destroy(GameManager.Instance.gameObject);
                                }
                                endChapterScreen.transform.SetSiblingIndex(50);
                                endChapterScreen.gameObject.SetActive(true);
                            });
            }
        }
        Debug.Log("MP : " + MP);
        Debug.Log("levelGoal : " + levelGoal);
        Debug.Log("FirstTime : " + PlayerPrefs.GetInt("FirstTime"));
        if (!MP && levelGoal != null && PlayerPrefs.GetInt("FirstTime") != 0)
        {
            string levelName = SceneManager.GetActiveScene().name;

            bool firstTimeBeat = PlayerPrefs.GetInt(levelName + "_beaten", 0) == 0;

            int xpReward = firstTimeBeat ? levelGoal.xp : 0;
            int trophyReward = PlayerPrefs.GetInt("LastRunTrophyReward", 0);
            if (levelGoal.trophies != 0)
                StartCoroutine(GenerateGains(xpReward, trophyReward));
        }
        if (QuestRotator.Instance != null && PlayerPrefs.GetInt("FirstTime") != 0 && PlayerPrefs.GetInt("Level") > 30)
            StartCoroutine(SpawnQuestProgressPrefabs(QuestRotator.Instance.updatedQuests));
        string currentLevelName = SceneManager.GetActiveScene().name;
        if (!CheckIfLevelIsBeaten())
        {
            Debug.Log("Increasing Level from " + PlayerPrefs.GetInt("Level") + " to " + (PlayerPrefs.GetInt("Level") + 1));
            PlayerPrefs.SetInt("Level", PlayerPrefs.GetInt("Level") + 1);
        }
        if (levelGoal != null && levelGoal.levelProgress != null)
        {
            levelProgress = Instantiate(levelGoal.levelProgress, this.transform);
            levelProgress.levelGoal = levelGoal;
            levelProgress.Initialize();
            uIParticle = GetComponentInChildren<UIParticle>();
            levelProgress.uiParticle = uIParticle;
            levelProgress.winScreen = this;
        }
        else
        {
            // Fallback if levelProgress is missing — allow level progression
            Debug.Log("[WinScreen] No LevelProgress assigned. Automatically activating next level button.");
            ActivateButtons();
        }
        if (playerGameObject != null)
        {
            originalScale = playerGameObject.localScale; // Store original scale
        }
        if (levelGoal.xp == 0) panelXP.SetActive(false);
        if (levelGoal.trophies == 0) panelTrophies.SetActive(false);
        StartCoroutine(MoveAndScalePlayerEmoteObject());
        if (PlayerPrefs.GetInt("IsSeparateBuild") == 1)
        {
            currentTime.gameObject.SetActive(true);
            totalTime.gameObject.SetActive(true);
            float totalPlayTime = PlayerPrefs.GetFloat("TotalTimePlayed", 0);
            float currentPlayTime = levelGoal.currentTime;
            currentTime.text = "Current Time: " + FormatTime(currentPlayTime);
            PlayerPrefs.SetFloat("TotalTimePlayed", totalPlayTime + currentPlayTime);
            totalPlayTime = PlayerPrefs.GetFloat("TotalTimePlayed", 0);
            totalTime.text = "Total Time: " + FormatTime(totalPlayTime);
        }
        else
        {

            currentTime.gameObject.SetActive(false);
            totalTime.gameObject.SetActive(false);
        }


        if (levelCompleteText != null)
        {
            levelCompleteText.text = "Level " + ((SceneManager.GetActiveScene().buildIndex + 1) - 3).ToString() + " Complete!";
        }

        PlayerPrefs.SetInt(currentLevelName + "_beaten", 1);
        PlayerPrefs.Save();
    }
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60F);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60F);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    private bool CheckIfLevelIsBeaten()
    {

        {
            string currentLevelName = SceneManager.GetActiveScene().name;

            // Check if this level has been beaten before
            if (PlayerPrefs.GetInt(currentLevelName + "_beaten", 0) == 0)
            {
                // This is the first time this level is beaten, grant rewards!
                Debug.Log("Level " + currentLevelName + " beaten for the first time! Granting rewards.");
                // Grant your rewards here (e.g., coins, XP, unlocks)


                return false;
            }
            else
            {
                // This level has been beaten before, it's a replay. No new rewards.
                Debug.Log("Level " + currentLevelName + " replayed. No new rewards.");
                return true;
            }

            // ... (any other logic for level completion, like loading next scene)
        }

    }
    private Vector3 originalScale;
    public IEnumerator MoveAndScalePlayerEmoteObject()
    {
        yield return new WaitForSeconds(1.5f); // Wait for animation

        if (playerGameObject != null)
        {
            float screenWidth = Screen.width;
            float targetX = screenWidth * 0.32f;
            Vector3 currentPosition = playerGameObject.position;
            Vector3 targetPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);
            Vector3 targetScale = originalScale * 0.75f; // Half the original scale

            // Use smooth movement and scaling with DOTween (in parallel)
            playerGameObject.DOMove(targetPosition, 1f).SetEase(Ease.OutQuad).Play(); // Add easing if you want

            playerGameObject.DOScale(targetScale, 1f).SetEase(Ease.OutQuad).Play(); // Add easing if you want


        }
        else
        {
            Debug.LogError("playerGameObject is not assigned!");

        }

    }

    public List<QuestType> uniqueQuestType = new();
    public List<QuestData> uniqueQuestData = new();
    private IEnumerator SpawnQuestProgressPrefabs(List<QuestData> questDataList)
    {

        foreach (QuestData questData in questDataList)
        {
            if (!uniqueQuestType.Contains(questData.questType))
            {
                uniqueQuestType.Add(questData.questType);
                uniqueQuestData.Add(questData);
            }
            else Debug.Log("We already have this tpye of quest");
        }
        yield return new WaitForSeconds(0.3f);
        int length = (uniqueQuestData.Count) >= 3 ? 3 : uniqueQuestData.Count;
        for (int i = 0; i < length; i++)
        {
            yield return new WaitForSeconds(0.3f);
            GameObject questPanel = Instantiate(questPanelPrefab, parentPanel.transform);
            questPanel.GetComponent<Quest>().questData = uniqueQuestData[i];
            questPanel.transform.DOPunchScale(new Vector3(questPanel.transform.localScale.x, questPanel.transform.localScale.y, questPanel.transform.localScale.z), 0.3f, 5, 1).Play();
        }

    }
    public IEnumerator GenerateGains(int xpGains, int trophyGains)
    {
        Debug.Log("Generating gains and proceednig");
        if (levelGoal.bonusUnlocked)
        {
            trophyGains += levelGoal.BONUS_TROPHIES;
        }

        if (xpGains <= 0 && trophyGains <= 0)
        {
            XPText.text = "0";
            TrophyText.text = "0";
            yield break;
        }
        float blingsCount = 5; // Number of times the bling sound will play
        float xpIncrement = (float)xpGains / blingsCount; // Increment for each bling sound
        float trophyIncrement = (float)trophyGains / blingsCount; // Increment for each bling sound
        Debug.Log("xpIncrement :" + xpIncrement);
        // Calculate the time interval between bling sounds
        float blingInterval;
        float blingIntervalTrophies;
        if (audioSourceXP != null)
        {

            blingInterval = (audioSourceXP.clip.length / blingsCount) / 5;
        }
        else blingInterval = 0.5f;
        if (audioSourceTrophies != null)
        {
            blingIntervalTrophies = (audioSourceTrophies.clip.length / blingsCount) / 2;
        }
        else blingIntervalTrophies = 0.5f;
        PlayerPrefs.SetInt("XPGain", xpGains + PlayerPrefs.GetInt("XPGain"));
        PlayerPrefs.SetInt("TrophyGain", trophyGains + PlayerPrefs.GetInt("TrophyGain"));
        Debug.Log("Length: " + blingInterval);
        int xpAmount = 0;
        float remainderGains = (float)xpGains % blingsCount;
        if (xpGains > 0)
        {

            for (int i = 0; i < blingsCount; i++)
            {
                // Wait for the next bling interval
                yield return new WaitForSeconds(blingInterval + 0.1f);

                // Increase the XP gains by the bling increment

                xpAmount += Mathf.RoundToInt(xpIncrement);
                Debug.Log("XPGains: " + xpIncrement);

                // Cap the XP gains to the maximum
                if (xpAmount > xpGains)
                    xpAmount = xpGains;

                // Update the XP text\
                if (i == blingsCount - 1)
                    xpAmount += (int)remainderGains;
                Debug.Log("XP Amount: " + xpAmount);
                XPText.text = xpAmount.ToString();
                // Play the bling sound
                if (audioSourceXP != null)

                    audioSourceXP.Play();
                XPText.transform.DOPunchScale(new Vector3(XPText.transform.localScale.x + 0.01f, XPText.transform.localScale.y + 0.01f, XPText.transform.localScale.y + 0.01f), blingInterval).Play();
            }
            yield return new WaitForSeconds(1f);
        }

        int trophyAmount = 0;
        Debug.Log("Trophy increment :" + trophyIncrement);
        float remainderTrophyGains = (float)trophyGains % blingsCount; // Add this line

        if (trophyGains > 0)
        {
            for (int i = 0; i < blingsCount; i++)
            {
                yield return new WaitForSeconds(blingIntervalTrophies + 0.1f);

                trophyAmount += Mathf.RoundToInt(trophyIncrement);
                Debug.Log("Trophy Amount (Iteration " + i + "): " + trophyAmount); // Add this line

                if (trophyAmount > trophyGains)
                    trophyAmount = trophyGains;

                if (i == blingsCount - 1)
                    trophyAmount += (int)remainderTrophyGains; // Add this line

                TrophyText.text = trophyAmount.ToString();

                if (audioSourceTrophies != null)
                    audioSourceTrophies.Play();

                TrophyText.transform.DOPunchScale(new Vector3(TrophyText.transform.localScale.x + 0.01f, TrophyText.transform.localScale.y + 0.01f, TrophyText.transform.localScale.y + 0.01f), blingInterval).Play();
            }
        }
        int starsEarned = 0;
        float runTime = levelGoal.currentTime; // or however you measure it
        if (runTime <= levelGoal.threeStarTime) starsEarned = 3;
        else if (runTime <= levelGoal.twoStarTime) starsEarned = 2;
        else if (runTime <= levelGoal.oneStarTime) starsEarned = 1;

        // Save stars only if better than previous
        string levelKey = SceneManager.GetActiveScene().name + "_Stars";
        int previousStars = PlayerPrefs.GetInt(levelKey, 0);
        if (starsEarned > previousStars)
        {
            PlayerPrefs.SetInt(levelKey, starsEarned);
        }
        starsEarnedTotal = starsEarned;

        PlayerPrefs.Save();
    }
    public int starsEarnedTotal;
    public void Proceed()
    {
        StartCoroutine(ProceedCoroutine());
    }
    public IEnumerator ProceedCoroutine()
    {
        Debug.Log("Generating gains and proceeding");
        proceedButton.SetActive(false);
        if (rewardsPanel != null)
        {
            rewardsPanel.SetActive(true);
            rewardsPanel.transform.DOPunchScale(new Vector3(rewardsPanel.transform.localScale.x + 0.03f, rewardsPanel.transform.localScale.y + 0.03f, rewardsPanel.transform.localScale.z + 0.03f), 0.3f, 1, 0).Play();
        }
        yield return new WaitForSeconds(1f);
        // if (PlayerPrefs.GetInt("FirstTime") != 0 && !CheckIfLevelIsBeaten()) StartCoroutine(GenerateGains(levelGoal.xp, levelGoal.trophies));
        yield return new WaitForSeconds(5f);
        // mainMenuButton.gameObject.SetActive(true);
        // mainMenuButton.transform.DOPunchScale(new Vector3(mainMenuButton.transform.localScale.x + 0.05f, mainMenuButton.transform.localScale.y + 0.05f, mainMenuButton.transform.localScale.z + 0.05f), 0.3f, 1, 0).Play();
    }
    public void ActivateButtons()
    {
        if (!GameManager.Instance.isFinalChapterLevel)
        {

            if (GameManager.Instance.ShouldHaveMainMenuButton)
            {
                proceedButton.SetActive(false);
                nextLevelButton.gameObject.SetActive(true);
                mainMenuButton.gameObject.SetActive(true);
            }
            else if (GameManager.Instance.SendsBackToMainMenu)
            {
                proceedButton.SetActive(true);
                nextLevelButton.gameObject.SetActive(false);
                mainMenuButton.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("Setting next level button to proceed button position");
                proceedButton.SetActive(false);
                mainMenuButton.gameObject.SetActive(false);
                nextLevelButton.transform.localPosition = proceedButton.transform.localPosition;
                nextLevelButton.GetComponent<PopoutButton>().startScale = proceedButton.transform.localScale;
                nextLevelButton.GetComponent<PopoutButton>().wasScaleAssigned = true;
                nextLevelButton.gameObject.SetActive(true);
            }
        }
        else
        {
            proceedButton.SetActive(true);
            nextLevelButton.gameObject.SetActive(false);
            mainMenuButton.gameObject.SetActive(false);
        }

    }

}
