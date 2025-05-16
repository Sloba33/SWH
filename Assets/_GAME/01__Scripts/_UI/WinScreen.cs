using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using Coffee.UIExtensions;

public class WinScreen : MonoBehaviour
{
    public bool sceneOverride;
    public string sceneOverrideName = "01_MainMenu";
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


    private void Start()
    {
        AudioManager.Instance.BGMVolume = 0;
        playerEmoteObject = FindObjectOfType<Player>().gameObject;
        if (playerEmoteObject != null)
        {
            playerEmoteObject.GetComponent<Animator>().SetBool("AFK", false);
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

                  //   NetworkManager.Singleton.Shutdown();
                  //   if (NetworkManager.Singleton != null)
                  //   {
                  //       Destroy(NetworkManager.Singleton.gameObject);
                  //   }
                  if (GameManager.Instance != null)
                  {
                      Destroy(GameManager.Instance.gameObject);
                  }
                  //   if (AudioManager.Instance != null)
                  //   {
                  //     Destroy(AudioManager.Instance.gameObject);
                  //   }
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
            proceedButton.GetComponent<Button>().onClick.AddListener(() =>
                         {

                             if (GameManager.Instance != null)
                             {
                                 Destroy(GameManager.Instance.gameObject);
                             }

                             sceneLoader.LoadMainMenu();
                         });
        }
        Debug.Log("MP : " + MP);
        Debug.Log("levelGoal : " + levelGoal);
        Debug.Log("FirstTime : " + PlayerPrefs.GetInt("FirstTime"));
        if (!MP && levelGoal != null && PlayerPrefs.GetInt("FirstTime") != 0)
        {
            Debug.Log("levelGoal.xp : " + levelGoal.xp);
            Debug.Log("levelGoal.trophies  : " + levelGoal.trophies);

            StartCoroutine(GenerateGains(levelGoal.xp, levelGoal.trophies));
        }
        else
        {
            // if (proceedButton != null)
            // {
            //     proceedButton.SetActive(true);
            //     proceedButton.transform.DOPunchScale(new Vector3(proceedButton.transform.localScale.x + 0.03f, proceedButton.transform.localScale.y + 0.03f, proceedButton.transform.localScale.z + 0.03f), 0.3f, 1, 0).Play();
            // }

        }
        if (QuestRotator.Instance != null && PlayerPrefs.GetInt("FirstTime") != 0 && PlayerPrefs.GetInt("Level") > 30)
            StartCoroutine(SpawnQuestProgressPrefabs(QuestRotator.Instance.updatedQuests));
        PlayerPrefs.SetInt("Level", PlayerPrefs.GetInt("Level") + 1);
        if (levelGoal != null && levelGoal.levelProgress != null)
        {
            levelProgress = Instantiate(levelGoal.levelProgress, this.transform);
            levelProgress.levelGoal = levelGoal;
            levelProgress.Initialize();
            uIParticle = GetComponentInChildren<UIParticle>();
            levelProgress.uiParticle = uIParticle;
            levelProgress.winScreen = this;
        }
        if (playerGameObject != null)
        {
            originalScale = playerGameObject.localScale; // Store original scale
        }
        if (levelGoal.xp == 0) panelXP.SetActive(false);
        if (levelGoal.trophies == 0) panelTrophies.SetActive(false);
        StartCoroutine(MoveAndScalePlayerEmoteObject());
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
        if (levelGoal.bonusUnlocked)
        {
            trophyGains += levelGoal.BONUS_TROPHIES;
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
        if (levelGoal.xp > 0)
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

        if (levelGoal.trophies > 0)
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



    }
    public void Proceed()
    {
        StartCoroutine(ProceedCoroutine());
    }
    public IEnumerator ProceedCoroutine()
    {
        proceedButton.SetActive(false);
        if (rewardsPanel != null)
        {
            rewardsPanel.SetActive(true);
            rewardsPanel.transform.DOPunchScale(new Vector3(rewardsPanel.transform.localScale.x + 0.03f, rewardsPanel.transform.localScale.y + 0.03f, rewardsPanel.transform.localScale.z + 0.03f), 0.3f, 1, 0).Play();
        }
        yield return new WaitForSeconds(1f);
        if (PlayerPrefs.GetInt("FirstTime") != 0) StartCoroutine(GenerateGains(levelGoal.xp, levelGoal.trophies));
        yield return new WaitForSeconds(5f);
        // mainMenuButton.gameObject.SetActive(true);
        // mainMenuButton.transform.DOPunchScale(new Vector3(mainMenuButton.transform.localScale.x + 0.05f, mainMenuButton.transform.localScale.y + 0.05f, mainMenuButton.transform.localScale.z + 0.05f), 0.3f, 1, 0).Play();
    }
    public void ActivateButtons()
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

}
