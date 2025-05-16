using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Obstacles;
public class LevelGoal : MonoBehaviour
{

    private Settings settings;
    public LevelProgress levelProgress;
    public float currentTime = 0, bonusTime;
    public TutorialDialogue tutorialDialogue;
    [Header("Obstacle Spawning Settings")]
    public bool SpawnFallingObstacles;
    public List<SpawnableItem<Obstacle>> FallingObstacles = new List<SpawnableItem<Obstacle>>();
    public int obstaclesToSpawn;
    public float ObstacleSpawnFrequency;
    public int TotalObstaclesSpawned;
    public float delayBoxSpawn = 6f;
    [SerializeField] public int minObstacleSpawnHeight = 16, maxObstacleSpawnHeight = 20;
    [Header("Bomb Spawning Settings")]

    public bool SpawnFallingBombs;
    public List<SpawnableItem<GameObject>> FallingBombs = new List<SpawnableItem<GameObject>>();

    public int bombsToSpawn;
    public float delayBombSpawn = 10f;
    public float bombSpawnFrequency = 5f;
    [SerializeField] int minBombSpawnHeight = 10, maxBombSpawnHeight = 15;
    [Header("Cellectible Spawning Settings")]

    public bool SpawnFallingCollectibles;
    public List<SpawnableItem<GameObject>> FallingCollectibles = new List<SpawnableItem<GameObject>>();

    public int collectiblesToSpawn;
    public float delayCollectibleSpawn = 10f;
    public float collectibleSpawnFreqency = 5f;
    [SerializeField] int minCollectibleSpawnHeight = 10, maxCollectibleSpawnHeight = 15;
    [Header("Level Settings")]

    public List<Obstacle> ObstaclesToDestroy_Player = new List<Obstacle>();
    public List<Obstacle> ObstaclesToDestroy_AI = new List<Obstacle>();
    public List<Obstacles.ObstacleType> obstacleTypes;
    public bool DualLevel;
    public bool Tutorial, FinalTutorial;

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
    private IEnumerator Start()
    {
        tutorialDialogue = FindObjectOfType<TutorialDialogue>();
        if (Tutorial)
        {
            tutorialHandler = FindObjectOfType<TutorialHandler>();
            if (tutorialHandler != null)
                tutorialHandler.shouldGuide = true;

        }
        // AddObstaclesToList();  JUST ADD THESE MANUALLY FOR EVERY PLAYER
        FindAndAddTilesToList();
        bool levelCompleted = PlayerPrefs.GetInt("" + SceneManager.GetActiveScene().name + "_Completed", 0) == 1;
        if (levelCompleted)
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


        if (!FinalTutorial && PlayerPrefs.GetInt("Level") < 2)
        {
            Debug.Log("Setting FirsTime to 0");
            PlayerPrefs.SetInt("FirstTime", 0);
        }

        else if (PlayerPrefs.GetInt("Level") <3)
        {
            Debug.Log("Setting First time to 1, and first timemenu to 1");
            PlayerPrefs.SetInt("FirstTime", 1);
            PlayerPrefs.SetInt("FirstTimeMenu", 1);
            PlayerPrefs.SetInt("coins", 100);
        }


        if (bombUniversal != null)
        {
            spawnPosition = bombUniversal.transform.localPosition;
            spawnRotation = bombUniversal.transform.localRotation;
        }
        if (Tutorial)
            InvokeRepeating(nameof(RespawnBomb), 45, 10);

    }
    public GameObject bombUniversalPrefab;
    public GameObject bombUniversal;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
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

        foreach (var item in items)
        {
            totalWeight += item.weight;
        }

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0;

        foreach (var item in items)
        {
            cumulativeWeight += item.weight;
            if (randomValue <= cumulativeWeight)
            {
                return item.item;
            }
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
        if (!firstObstacleSpawn)
        {
            yield return new WaitForSeconds(initialDelay);
            firstObstacleSpawn = true;
        }
        for (int i = 0; i < obstaclesToSpawn; i++)
        {
            if (TotalObstaclesSpawned % 20 == 0 && spawnFrequency > 1 && TotalObstaclesSpawned > 1) { spawnFrequency--; ObstacleSpawnFrequency--; }
            // Debug.Log("TotalObstaclesSpawned : " + TotalObstaclesSpawned + " TotalObstaclesSpawned MOD : " + (TotalObstaclesSpawned % 20) + " Obstacle Spawn Frequency : " + spawnFrequency + " total obstacles spawned : " + TotalObstaclesSpawned);
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
        // InvokeRepeating(nameof(SpawnRandomFallingCollectible), bombSpawnFrequency, delay);

    }
    private IEnumerator SpawnBombs(float delay)
    {

        yield return new WaitForSeconds(1f);
        StartCoroutine(SpawnRandomFallingBomb(bombSpawnFrequency, delay));
        // InvokeRepeating(nameof(SpawnRandomFallingCollectible), bombSpawnFrequency, delay);

    }
    private bool firstCollectibleSpawn;
    private bool firstBombSpawn;
    public IEnumerator SpawnRandomFallingBomb(float spawnFrequency, float initialDelay)
    {

        if (!firstBombSpawn)
        {
            yield return new WaitForSeconds(initialDelay);
            firstBombSpawn = true;
        }

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

        if (!firstCollectibleSpawn)
        {
            yield return new WaitForSeconds(initialDelay);
            firstCollectibleSpawn = true;
        }

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
    private int dualLevelCounter = 0;

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

    public AudioSource correctObstacle;
    public bool bonusUnlocked;
    public Dictionary<ObstacleColor, int> destroyedObstacleCounts = new Dictionary<ObstacleColor, int>();
    public void RemoveObstacle(Obstacle obs)
    {
        if (ObstaclesToDestroy_Player.Contains(obs))
        {
            ObstaclesToDestroy_Player.Remove(obs);

            ObstacleCounter++;

            // Track destroyed obstacle colors
            if (destroyedObstacleCounts.ContainsKey(obs.obstacleColor)) // Assuming 'color' is the enum property in your Obstacle class
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

                // Now you can access the counts of destroyed obstacles:
                Debug.Log("Destroyed Obstacle Counts:");
                foreach (var kvp in destroyedObstacleCounts)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value}");
                }

                // Example: Check if at least 2 red obstacles were destroyed
                if (destroyedObstacleCounts.ContainsKey(ObstacleColor.Red) && destroyedObstacleCounts[ObstacleColor.Red] >= 2)
                {
                    Debug.Log("At least 2 red obstacles were destroyed!");
                    // Perform some action based on this condition.
                }

                // Example: Get the total number of obstacles destroyed for a specific color
                int redCount = destroyedObstacleCounts.ContainsKey(ObstacleColor.Red) ? destroyedObstacleCounts[ObstacleColor.Red] : 0;
                Debug.Log($"Red Obstacle Count: {redCount}");
                Debug.Log("Level Goal: All obstacles destroyed. Triggering OnLevelCompleted event.");

                // IMPROVED: Check for subscribers before invoking


            }

            if (PlayerPrefs.GetInt("Level") < 2)
            {
                StartCoroutine(AudioManager.Instance.PlayUISound("bling", 0.25f));
                Debug.Log("Playing Bling");
            }
        }
    }
    public void RemoveObstacleFromSection(Obstacle obs)
    {
        if (Tutorial)
        {
            for (int i = 0; i < ListOfGoalLists.list.Count; i++)
            {
                Debug.Log("Iterating...");
                for (int j = 0; j < ListOfGoalLists.list[i].list.Count; j++)
                {
                    if (ListOfGoalLists.list[i].list.Contains(obs))
                    {
                        Debug.Log("Obstacle found");
                        Debug.Log("" + ListOfGoalLists.list[i].list[j].name);
                        ListOfGoalLists.list[i].list.Remove(obs);
                        if (ListOfGoalLists.list[i].list.Count == 0)
                        {
                            Debug.Log("Section cleared");
                            StartCoroutine(StartNextStep());
                            // AudioManager.Instance.PlaySound(8);
                        }
                        break;
                    }
                    else
                    {

                        Debug.Log("No object found");

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

        if (settings == null)
            settings = FindObjectOfType<Settings>();
        PlayerController pc = FindObjectOfType<PlayerController>();
        Animator anim = pc.transform.GetComponent<Animator>();
        settings.gameWon = true;
        yield return new WaitForSeconds(delay);

        if (pc != null) pc.enabled = false;
        // if (anim != null) anim.enabled = false;
        settings.ActivateWinPanel();


    }
    public IEnumerator WinTutorial(float delay)
    {
        if (settings == null)
            settings = FindObjectOfType<Settings>();
        PlayerController pc = FindObjectOfType<PlayerController>();
        Animator anim = pc.transform.GetComponent<Animator>();
        settings.gameWon = true;
        yield return new WaitForSeconds(delay);
        NameSelector nameSelector = FindObjectOfType<NameSelector>(true);
        nameSelector.gameObject.SetActive(true);
        settings.controlsPanel.SetActive(false);


    }
    public IEnumerator LoseLevel()
    {
        yield return new WaitForSeconds(1.2f);
        if (settings != null) settings = FindObjectOfType<Settings>();
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
        // Using FindObjectsOfType to find all active objects of type Tile
        Tile[] tiles = FindObjectsOfType<Tile>();

        // Add each Tile object to the list
        foreach (Tile tile in tiles)
        {
            if (tile.gameObject.activeSelf)
                tileList.Add(tile);
        }

        // Print the count of Tile objects found in the scene
        Debug.Log("Number of Tile objects in the scene: " + tileList.Count);
    }
    public void TurnOnPullEvent(Component sender, object data)
    {
        if (tutorialHandler != null)
            tutorialHandler.shouldGuide = false;
        if (joystickHint != null) joystickHint.SetActive(false);
        pullButton.gameObject.SetActive(true);
        pullHint.gameObject.SetActive(true);

    }
    public void TurnOnJumpEvent(Component sender, object data)
    {
        Debug.Log("Triggered : " + sender.name + " and : " + data);
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
    public void ToggleHitOn(Component sender, object data)
    {
        hitHint.gameObject.SetActive(true);
    }
    public void ToggleHitOff(Component sender, object data)
    {
        hitHint.gameObject.SetActive(false);
    }
    public void ToggleHitDownOn(Component sender, object data)
    {
        hitDownHint.gameObject.SetActive(true);
    }
    public void ToggleHitDownOff(Component sender, object data)
    {
        hitDownHint.gameObject.SetActive(false);
    }
    public int currentStep = 0;
    public List<GameObject> tutorialBridges;
    public List<GameObject> tutorialBarriers;
    public List<GameObject> tutorialSpotlights;

    public bool nextStepStarted;
    public IEnumerator StartNextStep()
    {
        if (!nextStepStarted)
        {
            // if (joystickHint.activeSelf) joystickHint.SetActive(false);
            if (tutorialHandler != null)
            {

                tutorialHandler.shouldGuide = false;
                joystickHint.gameObject.SetActive(false);
            }
            nextStepStarted = true;
            int tempStep = currentStep;
            tempStep++;
            Debug.Log("temp step" + tempStep);
            if (tempStep > tutorialBarriers.Count)
            {
                Debug.Log("Congrats");
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
                    Debug.Log("What step");
                    if (currentStep == 1)
                    {
                        Debug.Log("playing well done");
                        tutorialDialogue.ToggleDialogue(DialogueType.WellDone);
                    }
                    else
                    {
                        Debug.Log("playing good job");
                        tutorialDialogue.ToggleDialogue(DialogueType.GoodJob);
                    }

                }
                else Debug.Log("No dialogue ref assigned");
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