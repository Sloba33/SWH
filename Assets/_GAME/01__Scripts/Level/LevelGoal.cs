using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LevelGoal : MonoBehaviour
{
    private Settings settings;
    public LevelProgress levelProgress;
    public float currentTime = 0, bonusTime;
    public TutorialDialogue tutorialDialogue;

    public bool SpawnFallingObstacles;
    public List<SpawnableItem<Obstacle>> FallingObstacles = new List<SpawnableItem<Obstacle>>();
    public int obstaclesToSpawn;
    public float ObstacleSpawnFrequency;
    public int TotalObstaclesSpawned;
    public float delayBoxSpawn = 6f;
    [SerializeField] public int minObstacleSpawnHeight = 16, maxObstacleSpawnHeight = 20;

    public bool SpawnFallingBombs;
    public List<SpawnableItem<GameObject>> FallingBombs = new List<SpawnableItem<GameObject>>();
    public int bombsToSpawn;
    public float delayBombSpawn = 10f;
    public float bombSpawnFrequency = 5f;
    [SerializeField] int minBombSpawnHeight = 10, maxBombSpawnHeight = 15;

    public bool SpawnFallingCollectibles;
    public List<SpawnableItem<GameObject>> FallingCollectibles = new List<SpawnableItem<GameObject>>();
    public int collectiblesToSpawn;
    public float delayCollectibleSpawn = 10f;
    public float collectibleSpawnFreqency = 5f;
    [SerializeField] int minCollectibleSpawnHeight = 10, maxCollectibleSpawnHeight = 15;

    public List<Obstacle> ObstaclesToDestroy_Player = new List<Obstacle>();
    public List<Obstacle> ObstaclesToDestroy_AI = new List<Obstacle>();
    public List<ObstacleType> obstacleTypes;
    public bool DualLevel;
    public bool Tutorial;
    public bool FinalTutorial;
    public bool IsIntroLevel;

    public Button pullButton;
    public Button jumpButton;
    public Button hit, hitDown;
    public GameObject pullHint, jumpHint, hitHint, hitDownHint, joystickHint, joystickHintJump;

    public int bombCount;
    public bool bombs, weapons, pull, jump;

    public int ObstacleCounter, ObstacleTotal;
    private List<Tile> tileList = new();
    public LevelType levelType;

    public int xp, trophies, BONUS_TROPHIES;
    public List<Obstacle> playerSideFallingObstacles = new List<Obstacle>();
    public List<Obstacle> AISideFallingObstacles = new List<Obstacle>();
    public float dualBoxSpawnDelay = 7f;
    private TutorialHandler tutorialHandler;

    public float fillPercentage;

    public GameObject bombUniversalPrefab;
    public GameObject bombUniversal;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    public AudioSource correctObstacle;
    public bool bonusUnlocked;
    public Dictionary<ObstacleColor, int> destroyedObstacleCounts = new Dictionary<ObstacleColor, int>();
    private int dualLevelCounter = 0;

    private const string PREF_GAMEPLAY_TUTORIAL_COMPLETED = "GameplayTutorialCompleted";
    private const string PREF_INTRO_MENU_TUTORIAL_STAGE = "IntroMenuTutorialStage";
    private const string PREF_CURRENT_INTRO_LEVEL = "Level";
    private const string PREF_FIRST_TIME = "FirstTime";

    private IEnumerator Start()
    {
        tutorialDialogue = FindObjectOfType<TutorialDialogue>();
        if (Tutorial)
        {
            tutorialHandler = FindObjectOfType<TutorialHandler>();
            if (tutorialHandler != null)
                tutorialHandler.shouldGuide = true;
        }

        FindAndAddTilesToList();

        bool levelCompletedPreviously = PlayerPrefs.GetInt(SceneManager.GetActiveScene().name + "_Completed", 0) == 1;
        if (levelCompletedPreviously)
        {
            xp = 0;
            trophies = 0;
        }

        if (DualLevel)
        {
            yield return new WaitForSeconds(dualBoxSpawnDelay);
            InvokeRepeating(nameof(SpawnDualBoxes), 5, 5);
        }
        else if (SpawnFallingObstacles)
        {
            StartCoroutine(SpawnBoxes(delayBoxSpawn));
        }
        if (SpawnFallingBombs)
        {
            StartCoroutine(SpawnBombs(delayBombSpawn));
        }
        if (SpawnFallingCollectibles)
        {
            StartCoroutine(SpawnCollectibles(delayCollectibleSpawn));
        }
        settings = FindObjectOfType<Settings>();

        if (bombUniversal != null)
        {
            spawnPosition = bombUniversal.transform.localPosition;
            spawnRotation = bombUniversal.transform.localRotation;
        }
        if (Tutorial)
            InvokeRepeating(nameof(RespawnBomb), 45, 10);

    }

    public void RespawnBomb()
    {
        if (bombUniversal == null)
        {
            bombUniversal = Instantiate(bombUniversalPrefab, spawnPosition, spawnRotation);
        }
    }

    private void FixedUpdate()
    {
        if (Tutorial && settings != null && !settings.gameWon && !settings.gameLost) return;
        currentTime += Time.deltaTime;
        if (settings != null && settings.timerText != null)
            settings.timerText.text = currentTime.ToString("F1");
    }

    private T GetRandomItemByWeight<T>(List<SpawnableItem<T>> items)
    {
        float totalWeight = 0;
        foreach (var item in items) { totalWeight += item.weight; }
        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0;
        foreach (var item in items)
        {
            cumulativeWeight += item.weight;
            if (randomValue <= cumulativeWeight) { return item.item; }
        }
        return default;
    }

    private IEnumerator SpawnBoxes(float delay)
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(SpawnRandomFallingBox(ObstacleSpawnFrequency, delay));
    }
    private bool firstObstacleSpawn;

    public IEnumerator SpawnRandomFallingBox(float spawnFrequency, float initialDelay)
    {
        if (!firstObstacleSpawn) { yield return new WaitForSeconds(initialDelay); firstObstacleSpawn = true; }
        for (int i = 0; i < obstaclesToSpawn; i++)
        {
            if (TotalObstaclesSpawned % 20 == 0 && spawnFrequency > 1 && TotalObstaclesSpawned > 1) { spawnFrequency--; ObstacleSpawnFrequency--; }
            TotalObstaclesSpawned++;
            int randomHeight = Random.Range(minObstacleSpawnHeight, maxObstacleSpawnHeight);
            int randomTile = Random.Range(0, tileList.Count);
            Obstacle obstacle = GetRandomItemByWeight(FallingObstacles);
            Vector3 spawnPos = new(tileList[randomTile].transform.position.x, randomHeight, tileList[randomTile].transform.position.z);
            Obstacle fallingObstacle = Instantiate(obstacle, spawnPos, obstacle.transform.rotation, null);
            fallingObstacle.name += TotalObstaclesSpawned.ToString();
            yield return new WaitForSeconds(spawnFrequency);
        }
    }
    private IEnumerator SpawnCollectibles(float delay)
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(SpawnRandomFallingCollectible(collectibleSpawnFreqency, delay));
    }
    private IEnumerator SpawnBombs(float delay)
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(SpawnRandomFallingBomb(bombSpawnFrequency, delay));
    }
    private bool firstCollectibleSpawn;
    private bool firstBombSpawn;

    public IEnumerator SpawnRandomFallingBomb(float spawnFrequency, float initialDelay)
    {
        if (!firstBombSpawn) { yield return new WaitForSeconds(initialDelay); firstBombSpawn = true; }
        for (int i = 0; i < bombsToSpawn; i++)
        {
            int randomHeight = Random.Range(minBombSpawnHeight, maxBombSpawnHeight);
            int randomTile = Random.Range(0, tileList.Count);
            GameObject objectToSpawn = GetRandomItemByWeight(FallingBombs);
            Vector3 spawnPos = new(tileList[randomTile].transform.position.x, randomHeight, tileList[randomTile].transform.position.z);
            GameObject collectible = Instantiate(objectToSpawn, spawnPos, objectToSpawn.transform.rotation, null);
            yield return new WaitForSeconds(spawnFrequency);
        }
    }
    public IEnumerator SpawnRandomFallingCollectible(float spawnFrequency, float initialDelay)
    {
        if (!firstCollectibleSpawn) { yield return new WaitForSeconds(initialDelay); firstCollectibleSpawn = true; }
        for (int i = 0; i < collectiblesToSpawn; i++)
        {
            int randomHeight = Random.Range(minCollectibleSpawnHeight, maxCollectibleSpawnHeight);
            int randomTile = Random.Range(0, tileList.Count);
            GameObject objectToSpawn = GetRandomItemByWeight(FallingCollectibles);
            Vector3 spawnPos = new(tileList[randomTile].transform.position.x, randomHeight, tileList[randomTile].transform.position.z);
            GameObject collectible = Instantiate(objectToSpawn, spawnPos, objectToSpawn.transform.rotation, null);
            yield return new WaitForSeconds(spawnFrequency);
        }
    }

    public void SpawnDualBoxes()
    {
        if (dualLevelCounter < playerSideFallingObstacles.Count)
        {
            playerSideFallingObstacles[dualLevelCounter].gameObject.SetActive(true);
            AISideFallingObstacles[dualLevelCounter].gameObject.SetActive(true);
            dualLevelCounter++;
        }
    }

    void AddObstaclesToList()
    {
        UnityEngine.Object[] objectsOfType = FindObjectsOfType(typeof(Obstacle));
        foreach (var obj in objectsOfType)
        {
            Obstacle currentObstacle = (Obstacle)obj;
            if (obstacleTypes.Contains(currentObstacle.obstacleType))
            {
                ObstaclesToDestroy_Player.Add(currentObstacle);
            }
        }
        ObstacleTotal = ObstaclesToDestroy_Player.Count;
    }

    public void RemoveObstacle(Obstacle obs)
    {
        if (ObstaclesToDestroy_Player.Contains(obs))
        {
            ObstaclesToDestroy_Player.Remove(obs);

            ObstacleCounter++;

            if (destroyedObstacleCounts.ContainsKey(obs.obstacleColor))
            {
                destroyedObstacleCounts[obs.obstacleColor]++;
            }
            else
            {
                destroyedObstacleCounts[obs.obstacleColor] = 1;
            }

            if (ObstaclesToDestroy_Player.Count == 0)
            {
                bonusUnlocked = currentTime < bonusTime;
                StartCoroutine(WinLevel(0.9f));
            }

            if (Tutorial)
            {
                StartCoroutine(AudioManager.Instance.PlayUISound("bling", 0.25f));
            }
        }
    }

    public void RemoveObstacleFromSection(Obstacle obs)
    {
        if (Tutorial)
        {
            for (int i = 0; i < ListOfGoalLists.list.Count; i++)
            {
                for (int j = 0; j < ListOfGoalLists.list[i].list.Count; j++)
                {
                    if (ListOfGoalLists.list[i].list.Contains(obs))
                    {
                        ListOfGoalLists.list[i].list.Remove(obs);
                        if (ListOfGoalLists.list[i].list.Count == 0)
                        {
                            StartCoroutine(StartNextStep());
                        }
                        break;
                    }
                }
            }
        }
        else
        {
            RemoveObstacle(obs);
        }
    }

    public IEnumerator WinLevel(float delay)
    {
        Debug.Log("Winning Level");
        if (settings == null) settings = FindObjectOfType<Settings>();
        PlayerController pc = FindObjectOfType<PlayerController>();
        settings.gameWon = true;
        yield return new WaitForSeconds(delay);

        if (pc != null) pc.enabled = false;
        settings.ActivateWinPanel();

        if (IsIntroLevel)
        {
            int currentIntroLevel = PlayerPrefs.GetInt(PREF_CURRENT_INTRO_LEVEL, 0);
            currentIntroLevel++;
            // PlayerPrefs.SetInt(PREF_CURRENT_INTRO_LEVEL, currentIntroLevel);
            PlayerPrefs.Save();

          
          
        }
    }

    public IEnumerator WinTutorial(float delay)
    {
        Debug.Log("Winning Tutorial");
        if (settings == null) settings = FindObjectOfType<Settings>();
        PlayerController pc = FindObjectOfType<PlayerController>();
        settings.gameWon = true;
        yield return new WaitForSeconds(delay);
        NameSelector nameSelector = FindObjectOfType<NameSelector>(true);
        nameSelector.gameObject.SetActive(true);
        settings.controlsPanel.SetActive(false);

        PlayerPrefs.SetInt(PREF_GAMEPLAY_TUTORIAL_COMPLETED, 1);
        PlayerPrefs.SetInt(PREF_INTRO_MENU_TUTORIAL_STAGE, (int)TutorialMenuManager.MenuTutorialStep.None);
        PlayerPrefs.SetInt(PREF_CURRENT_INTRO_LEVEL, 0);
        PlayerPrefs.SetInt(PREF_FIRST_TIME, 1);
        
        PlayerPrefs.Save();

        // if (GameFlowManager.Instance != null)
        // {
        //     GameFlowManager.Instance.LoadScene("01_MainMenu");
        // }
    }

    public IEnumerator LoseLevel()
    {
        yield return new WaitForSeconds(1.2f);
        if (settings == null) settings = FindObjectOfType<Settings>();
        settings.gameLost = true;
        if (!settings.gameWon)
        {
            settings.ActivateLosePanel();
        }
    }

    public void RespondToFlagEvent(Component sender, object data)
    {
        StartCoroutine(WinLevel(0.3f));
    }

    void FindAndAddTilesToList()
    {
        Tile[] tiles = FindObjectsOfType<Tile>();
        foreach (Tile tile in tiles)
        {
            if (tile.gameObject.activeSelf)
                tileList.Add(tile);
        }
    }

    public void TurnOnPullEvent(Component sender, object data)
    {
        if (tutorialHandler != null) tutorialHandler.shouldGuide = false;
        if (joystickHint != null) joystickHint.SetActive(false);
        pullButton.gameObject.SetActive(true);
        pullHint.gameObject.SetActive(true);
    }

    public void TurnOnJumpEvent(Component sender, object data)
    {
        pullHint.gameObject.SetActive(false);
        PlayerControls pc = FindObjectOfType<PlayerControls>();
        if (pc != null) pc.hintPull = null;
        jumpButton.gameObject.SetActive(true);
        jumpHint.SetActive(true);
        if (joystickHintJump != null) joystickHintJump.SetActive(true);
    }

    public void TurnOnHitEvent(Component sender, object data)
    {
        if (jumpHint != null) jumpHint.SetActive(false);
        if (hitDown != null) hitDown.gameObject.SetActive(true);
        if (hit != null) hit.gameObject.SetActive(true);
        Destroy(sender.gameObject, 0.05f);
    }

    public void ToggleHitOn(Component sender, object data) { hitHint.gameObject.SetActive(true); }
    public void ToggleHitOff(Component sender, object data) { hitHint.gameObject.SetActive(false); }
    public void ToggleHitDownOn(Component sender, object data) { hitDownHint.gameObject.SetActive(true); }
    public void ToggleHitDownOff(Component sender, object data) { hitDownHint.gameObject.SetActive(false); }

    public int currentStep = 0;
    public List<GameObject> tutorialBridges;
    public List<GameObject> tutorialBarriers;
    public List<GameObject> tutorialSpotlights;
    public bool nextStepStarted;

    public IEnumerator StartNextStep()
    {
        if (!nextStepStarted)
        {
            if (tutorialHandler != null)
            {
                tutorialHandler.shouldGuide = false;
                joystickHint.gameObject.SetActive(false);
            }
            nextStepStarted = true;
            int tempStep = currentStep;
            tempStep++;
            if (tempStep > tutorialBarriers.Count)
            {
                if (!Tutorial)
                    StartCoroutine(WinLevel(1f));
                else
                    StartCoroutine(WinTutorial(0.5f));
            }
            else
            {
                tutorialBridges[currentStep].SetActive(true);
                if (currentStep == 1) pullHint.gameObject.SetActive(false);
                yield return new WaitForSeconds(0.090f);
                tutorialBarriers[currentStep].SetActive(false);
                StartCoroutine(TurnOnSpotlight(currentStep));
                currentStep++;
                nextStepStarted = false;
                if (tutorialDialogue != null)
                {
                    int whatStep = currentStep % 2;
                    if (currentStep == 1)
                    {
                        tutorialDialogue.ToggleDialogue(DialogueType.WellDone);
                    }
                    else
                    {
                        tutorialDialogue.ToggleDialogue(DialogueType.GoodJob);
                    }
                }
                yield return new WaitForSeconds(0.25f);
                AudioManager.Instance.PlaySound(0);
            }
        }
    }

    public IEnumerator TurnOnSpotlight(int spotlightNumber)
    {
        yield return new WaitForSeconds(0.1f);
        tutorialSpotlights[spotlightNumber].SetActive(true);
        yield return new WaitForSeconds(0.1f);
        tutorialSpotlights[spotlightNumber].SetActive(false);
        yield return new WaitForSeconds(0.1f);
        tutorialSpotlights[spotlightNumber].SetActive(true);
    }

    public enum LevelType
    {
        Move, Pull, Jump, Hit, Bomb
    }
    public GoalList ListOfGoalLists = new GoalList();
}

[System.Serializable]
public class Goal
{
    public List<Obstacle> list;
}
[System.Serializable]
public class GoalList
{
    public List<Goal> list;
}
[System.Serializable]
public class SpawnableItem<T>
{
    public T item;
    public float weight;

    public SpawnableItem(T item, float weight)
    {
        this.item = item;
        this.weight = weight;
    }
}