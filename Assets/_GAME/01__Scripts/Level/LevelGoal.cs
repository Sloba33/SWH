using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class LevelGoal : MonoBehaviour
{
    private Settings settings;
    public LevelProgress levelProgress;
    public float currentTime = 0, bonusTime;
    public TutorialDialogue tutorialDialogue;
    [Header("Obstacle Spawn Settings")]
    public Dictionary<System.Tuple<ObstacleType, ObstacleColor>, Obstacle> goalObstaclePrefabs = new();
    private Dictionary<System.Tuple<ObstacleType, ObstacleColor>, Obstacle> _obstacleTemplates = new();
    public Dictionary<System.Tuple<ObstacleType, ObstacleColor>, int> initialObstacleCounts = new();
    private HashSet<System.Tuple<ObstacleType, ObstacleColor>> suppressedSpawnTypes = new();
    private List<Obstacle> _obstaclesDestroyedThisFrame = new List<Obstacle>();
    public bool SpawnFallingObstacles;
    public List<SpawnableItem<Obstacle>> FallingObstacles = new List<SpawnableItem<Obstacle>>();
    public int obstaclesToSpawn;
    public float ObstacleSpawnFrequency;
    public int TotalObstaclesSpawned;
    public float delayBoxSpawn = 6f;
    [SerializeField] public int minObstacleSpawnHeight = 16, maxObstacleSpawnHeight = 20;
    [Header("Bomb Spawn Settings")]
    public bool SpawnFallingBombs;
    public List<SpawnableItem<GameObject>> FallingBombs = new List<SpawnableItem<GameObject>>();
    public int bombsToSpawn;
    public float delayBombSpawn = 10f;
    public float bombSpawnFrequency = 5f;
    [SerializeField] int minBombSpawnHeight = 10, maxBombSpawnHeight = 15;
    [Header("Collectible Spawn Settings")]
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
        AddObstaclesToInitialCounts();
        // PopulateGoalObstaclePrefabs();
        CreateObstacleTemplates();

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
    public void QueueObstacleForSpawnProcessing(Obstacle obstacle)
    {
        if (obstacle != null && !_obstaclesDestroyedThisFrame.Contains(obstacle))
        {
            _obstaclesDestroyedThisFrame.Add(obstacle);
            Debug.Log($"LevelGoal: Queued obstacle {obstacle.name} for spawn processing. Queue size: {_obstaclesDestroyedThisFrame.Count}");
        }
    }
    private void ProcessDestroyedObstaclesInternal() // Renamed to avoid conflict with potential public usage
    {
        if (_obstaclesDestroyedThisFrame.Count == 0)
        {
            return; // Nothing to process
        }

        // Use a HashSet to ensure we only check each unique type/color once per batch
        HashSet<System.Tuple<ObstacleType, ObstacleColor>> uniqueDestroyedTypesAndColors = new();

        // Populate the HashSet from the obstacles queued this frame
        foreach (var obstacle in _obstaclesDestroyedThisFrame)
        {
            if (obstacle != null)
            {
                uniqueDestroyedTypesAndColors.Add(System.Tuple.Create(obstacle.obstacleType, obstacle.obstacleColor));
            }
        }

        // NEW LOGS for debugging the "2x2" issue
        string uniquePairsString = string.Join(", ", uniqueDestroyedTypesAndColors.Select(t => $"{t.Item1}-{t.Item2}"));
        Debug.Log($"ProcessDestroyedObstaclesInternal: Found {uniqueDestroyedTypesAndColors.Count} unique types/colors: [{uniquePairsString}]");

        // Iterate through unique types and trigger spawn checks
        foreach (var typeColorPair in uniqueDestroyedTypesAndColors)
        {
            Debug.Log($"ProcessDestroyedObstaclesInternal: Calling CheckForMissingObstaclesAndSpawn for {typeColorPair.Item1}-{typeColorPair.Item2}");
            CheckForMissingObstaclesAndSpawn(typeColorPair.Item1, typeColorPair.Item2);
        }

        // Clear the queue for the next frame
        _obstaclesDestroyedThisFrame.Clear();
        Debug.Log("ProcessDestroyedObstaclesInternal: Finished processing batch and cleared queue.");
    }

    private void AddObstaclesToInitialCounts()
    {
        initialObstacleCounts.Clear(); // Clear it first to ensure fresh counts

        // Iterate through the ObstaclesToDestroy_Player list.
        // This list should already contain all initial goal obstacles present in the scene at start.
        foreach (Obstacle obs in ObstaclesToDestroy_Player)
        {
            if (obs == null) continue; // Skip if reference is null (e.g., already destroyed or invalid)

            System.Tuple<ObstacleType, ObstacleColor> key = System.Tuple.Create(obs.obstacleType, obs.obstacleColor);
            if (initialObstacleCounts.ContainsKey(key))
            {
                initialObstacleCounts[key]++;
            }
            else
            {
                initialObstacleCounts[key] = 1;
            }
        }
    }
    private void CreateObstacleTemplates()
    {
        _obstacleTemplates.Clear(); // Clear any previous templates

        // Iterate through the unique obstacle types/colors identified as part of the level goal
        foreach (var entry in initialObstacleCounts)
        {
            System.Tuple<ObstacleType, ObstacleColor> neededKey = entry.Key;

            // Find an existing *scene instance* of an obstacle that matches this type/color.
            // We use ObstaclesToDestroy_Player as it should contain initial goal obstacles.
            Obstacle foundInstance = null;
            foreach (Obstacle obs in ObstaclesToDestroy_Player)
            {
                if (obs != null && obs.obstacleType == neededKey.Item1 && obs.obstacleColor == neededKey.Item2)
                {
                    foundInstance = obs;
                    break; // Found one, no need to search further
                }
            }

            if (foundInstance == null)
            {
                Debug.LogWarning($"No existing scene instance found for obstacle type {neededKey.Item1}-{neededKey.Item2}. Cannot create a template for respawning. Please ensure at least one of each goal obstacle type is present in the scene at start.");
                continue; // Cannot create a template if no source instance exists
            }

            // Create a clone of the found instance to use as a template
            // Instantiating from a scene object creates a copy of that scene object.
            Obstacle template = Instantiate(foundInstance);

            // Disable the template object immediately so it's not visible or interactive
            template.gameObject.SetActive(false);
            // Optionally, parent it under LevelGoal's GameObject for scene organization
            template.transform.SetParent(this.transform);
            template.name = $"Template_{neededKey.Item1}_{neededKey.Item2}";

            _obstacleTemplates.Add(neededKey, template);
            Debug.Log($"Created template for {neededKey.Item1}-{neededKey.Item2}");
        }
    }




    public void RespawnBomb()
    {
        if (bombUniversal == null)
        {
            bombUniversal = Instantiate(bombUniversalPrefab, spawnPosition, spawnRotation);
        }
    }

    void LateUpdate()
    {
        ProcessDestroyedObstaclesInternal();
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
    public void ProcessDestroyedObstacles(IEnumerable<Obstacle> destroyedObstacles)
    {
        HashSet<System.Tuple<ObstacleType, ObstacleColor>> uniqueDestroyedTypesAndColors = new();

        // NEW LOG
        Debug.Log($"ProcessDestroyedObstacles: Called with {destroyedObstacles.Count()} obstacles in the batch.");

        foreach (var obstacle in destroyedObstacles)
        {
            if (obstacle != null)
            {
                uniqueDestroyedTypesAndColors.Add(System.Tuple.Create(obstacle.obstacleType, obstacle.obstacleColor));
            }
        }

        // NEW LOG
        string uniquePairsString = string.Join(", ", uniqueDestroyedTypesAndColors.Select(t => $"{t.Item1}-{t.Item2}"));
        Debug.Log($"ProcessDestroyedObstacles: Found {uniqueDestroyedTypesAndColors.Count} unique types/colors: [{uniquePairsString}]");


        foreach (var typeColorPair in uniqueDestroyedTypesAndColors)
        {
            Debug.Log($"ProcessDestroyedObstacles: Calling CheckForMissingObstaclesAndSpawn for {typeColorPair.Item1}-{typeColorPair.Item2}");
            CheckForMissingObstaclesAndSpawn(typeColorPair.Item1, typeColorPair.Item2);
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
            if (!obs.isSpawnedForRecovery)
            {
                // Count how many of the same type/color are left
                int remaining = ObstaclesToDestroy_Player.Count(o =>
                    o != null &&
                    o.obstacleType == obs.obstacleType &&
                    o.obstacleColor == obs.obstacleColor &&
                    !o.isSpawnedForRecovery);
                Debug.Log($"[REMOVE] Removed obstacle {obs.name} of type {obs.obstacleType} and color {obs.obstacleColor}. Remaining: {remaining}");

                if (remaining == 0)
                {
                    DestroyAllRecoveryObstaclesOfType(obs.obstacleType, obs.obstacleColor);
                }
            }

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
    private void DestroyAllRecoveryObstaclesOfType(ObstacleType type, ObstacleColor color)
    {
        var recoveryObstacles = ObstaclesToDestroy_Player.Where(o =>
            o != null &&
            o.obstacleType == type &&
            o.obstacleColor == color &&
            o.isSpawnedForRecovery).ToList();
        bool anyGrounded = recoveryObstacles.Any(o => o.grounded);
        if (anyGrounded)
        {
            int countOnScene = ObstaclesToDestroy_Player.Count(o =>
            o != null &&
            o.obstacleType == type &&
            o.obstacleColor == color);
            int neededToReachThree = 3 - countOnScene;
            Debug.Log("Needed to reach 3 for grounded ones :" + neededToReachThree);

            if (neededToReachThree > 0)
            {
                var keySpawner = System.Tuple.Create(type, color);
                if (_obstacleTemplates.TryGetValue(keySpawner, out Obstacle templateObstacle) && templateObstacle != null)
                {
                    Debug.Log($"[SPAWN] Spawning {neededToReachThree} more {type}-{color} because only {countOnScene} remain.");
                    for (int i = 0; i < neededToReachThree; i++)
                    {
                        StartCoroutine(SpawnSpecificFallingObstacle(templateObstacle, ObstacleSpawnFrequency, 0f));
                    }
                }
            }
        }
        // foreach (var obstacle in recoveryObstacles)
        // {
        //     // Mark as processed
        //     ObstaclesToDestroy_Player.Remove(obstacle);

        //     // Play destruction
        //     if (!obstacle.queuedForDestruction)
        //     {
        //         obstacle.ParticleDestroy(Obstacle.ObstacleDestructionSource.Other);
        //     }
        // }
        var key = System.Tuple.Create(type, color);
        suppressedSpawnTypes.Add(key);
        Debug.Log($"Destroyed {recoveryObstacles.Count} recovery obstacles for {type}-{color} after final original was destroyed.");
    }
    // private void DestroyAllRecoveryObstaclesOfType(ObstacleType type, ObstacleColor color)
    // {
    //     var recoveryObstacles = ObstaclesToDestroy_Player.Where(o =>
    //         o != null &&
    //         o.obstacleType == type &&
    //         o.obstacleColor == color &&
    //         o.isSpawnedForRecovery).ToList();
    //     Debug.Log($"CDOS: [CLEANUP] Found {recoveryObstacles.Count} recovery obstacles of type {type} and color {color}.");

    //     bool anyGrounded = recoveryObstacles.Any(o => o.grounded);

    //     if (anyGrounded)
    //     {
    //         Debug.Log($"CDOS: [KEEP] Some recovery obstacles of {type}-{color} are grounded. Keeping all.");
    //         // Just check if we still need to spawn more
    //         int countOnScene = ObstaclesToDestroy_Player.Count(o =>
    //             o != null &&
    //             o.obstacleType == type &&
    //             o.obstacleColor == color);

    //         int neededToReachThree = 3 - countOnScene;

    //         if (neededToReachThree > 0)
    //         {
    //             var key = System.Tuple.Create(type, color);
    //             if (_obstacleTemplates.TryGetValue(key, out Obstacle templateObstacle) && templateObstacle != null)
    //             {
    //                 Debug.Log($"CDOS: [SPAWN] Spawning {neededToReachThree} more {type}-{color} because only {countOnScene} remain.");
    //                 for (int i = 0; i < neededToReachThree; i++)
    //                 {
    //                     StartCoroutine(SpawnSpecificFallingObstacle(templateObstacle, ObstacleSpawnFrequency, 0f));
    //                 }
    //             }
    //         }
    //     }
    //     else
    //     {
    //         Debug.Log($"CDOS: [CLEANUP] No recovery obstacles of {type}-{color} have landed. Destroying all recovery ones.");
    //         foreach (var obstacle in recoveryObstacles)
    //         {
    //             if (!obstacle.queuedForDestruction)
    //             {
    //                 ObstaclesToDestroy_Player.Remove(obstacle);
    //                 obstacle.ParticleDestroy(Obstacle.ObstacleDestructionSource.Other);
    //             }
    //         }
    //         Debug.Log($"CDOS: [CLEANUP] Destroyed {recoveryObstacles.Count} recovery obstacles of type {type} and color {color}.");
    //         // After destruction, spawn 3 new ones
    //         var key = System.Tuple.Create(type, color);
    //         if (_obstacleTemplates.TryGetValue(key, out Obstacle templateObstacle) && templateObstacle != null)
    //         {
    //             for (int i = 0; i < 3; i++)
    //             {
    //                 StartCoroutine(SpawnSpecificFallingObstacle(templateObstacle, ObstacleSpawnFrequency, 0f));
    //             }
    //         }
    //     }
    //     var keyRec = System.Tuple.Create(type, color);
    //     suppressedSpawnTypes.Add(keyRec);
    // }
    private void CheckForMissingObstaclesAndSpawn(ObstacleType destroyedType, ObstacleColor destroyedColor)
    {
        var keySuppressed = System.Tuple.Create(destroyedType, destroyedColor);
        if (suppressedSpawnTypes.Contains(keySuppressed))
        {
            Debug.Log($"CDOS: [SPAWN BLOCKED] Skipping spawn for {destroyedType}-{destroyedColor} due to recovery cleanup.");
            return;
        }
        // NEW LOG
        Debug.Log($"CDOS: Start for Type={destroyedType}, Color={destroyedColor}");

        if (!Tutorial)
        {
            int remainingCountForTypeColor = ObstaclesToDestroy_Player
                                                 .Count(o => o != null && o.obstacleType == destroyedType && o.obstacleColor == destroyedColor);

            System.Tuple<ObstacleType, ObstacleColor> key = System.Tuple.Create(destroyedType, destroyedColor);
            if (!initialObstacleCounts.TryGetValue(key, out int initialCount))
            {
                Debug.LogWarning($"CDOS: Initial count for {destroyedType}-{destroyedColor} not found. Cannot determine if respawn is needed.");
                return;
            }

            // NEW LOGS
            Debug.Log($"CDOS: Remaining for {destroyedType}-{destroyedColor}: {remainingCountForTypeColor}");
            Debug.Log($"CDOS: Initial count for {destroyedType}-{destroyedColor}: {initialCount}");


            if (remainingCountForTypeColor < 3 && initialCount >= 3)
            {
                int neededToReachThree = 3 - remainingCountForTypeColor;
                if (neededToReachThree > 0)
                {
                    Debug.Log($"CDOS: Spawning {neededToReachThree} of {destroyedType}-{destroyedColor} to enable match-3.");

                    System.Tuple<ObstacleType, ObstacleColor> templateKey = System.Tuple.Create(destroyedType, destroyedColor);
                    if (_obstacleTemplates.TryGetValue(templateKey, out Obstacle templateObstacle))
                    {
                        if (templateObstacle == null)
                        {
                            Debug.LogError($"CDOS Template obstacle for {destroyedType}-{destroyedColor} is null/destroyed! This should not happen.");
                            return;
                        }

                        // NEW LOG
                        Debug.Log($"CDOS: About to loop {neededToReachThree} times for spawning {templateObstacle.name}.");
                        for (int i = 0; i < neededToReachThree; i++)
                        {
                            // NEW LOG
                            Debug.Log($"CDOS: Spawning iteration {i + 1} of {neededToReachThree} for {templateObstacle.name}");
                            StartCoroutine(SpawnSpecificFallingObstacle(templateObstacle, ObstacleSpawnFrequency, 0f));
                            Debug.Log("$CDOS: Spawned a new obstacle of type {destroyedType} and color {destroyedColor} and name {templateObstacle.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"CDOS Could not find template for {destroyedType}-{destroyedColor}. Make sure an initial instance exists in the scene and CreateObstacleTemplates ran correctly.");
                    }
                }
            }
        }
    }
    private void FindAndAddTilesToList()
    {
        // Example: Find all obstacles in the scene. You might need to filter these.
        ObstaclesToDestroy_Player = new List<Obstacle>(FindObjectsOfType<Obstacle>());

        // Example: Populate tileList
        tileList = new List<Tile>(FindObjectsOfType<Tile>());
    }
    // private Obstacle GetSpecificSpawnableObstacle(ObstacleType type, ObstacleColor color)
    // {
    //     foreach (var spawnable in FallingObstacles)
    //     {
    //         if (spawnable.item.obstacleType == type && spawnable.item.obstacleColor == color)
    //         {
    //             return spawnable.item;
    //         }
    //         // Handle Universal type specifically if needed in this lookup context
    //         if (spawnable.item.obstacleType == ObstacleType.Universal && (type != ObstacleType.None))
    //         {
    //             // If a Universal obstacle can become any type/color, this logic needs careful consideration.
    //             // For now, assume a Universal obstacle can fulfill a missing requirement.
    //             // You might need a more sophisticated mapping here if Universal has specific 'target' types it can substitute for.
    //             return spawnable.item;
    //         }
    //     }
    //     return null; // No suitable spawnable obstacle found
    // }
    public IEnumerator SpawnSpecificFallingObstacle(Obstacle obstaclePrefab, float spawnFrequency, float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        int randomHeight = Random.Range(minObstacleSpawnHeight, maxObstacleSpawnHeight);
        int randomTile = Random.Range(0, tileList.Count);
        Vector3 spawnPos = new(tileList[randomTile].transform.position.x, randomHeight, tileList[randomTile].transform.position.z);
        Obstacle fallingObstacle = Instantiate(obstaclePrefab, spawnPos, obstaclePrefab.transform.rotation, null);
        fallingObstacle.isSpawnedForRecovery = true;
        fallingObstacle.gameObject.SetActive(true);
        // You might want a better naming convention here for spawned obstacles
        fallingObstacle.name = $"Spawned_{obstaclePrefab.name}_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
        // Important: Re-add this newly spawned obstacle to the ObstaclesToDestroy_Player list
        // so it counts towards the level goal.
        ObstaclesToDestroy_Player.Add(fallingObstacle);

        yield return new WaitForSeconds(spawnFrequency);
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

    // void FindAndAddTilesToList()
    // {
    //     Tile[] tiles = FindObjectsOfType<Tile>();
    //     foreach (Tile tile in tiles)
    //     {
    //         if (tile.gameObject.activeSelf)
    //             tileList.Add(tile);
    //     }
    // }

    public void TurnOnPullEvent(Component sender, object data)
    {
        if (tutorialHandler != null) tutorialHandler.shouldGuide = false;
        if (joystickHint != null) joystickHint.SetActive(false);
        pullButton.gameObject.SetActive(true);
        pullHint.gameObject.SetActive(true);
    }

    public void TurnOnJumpEvent(Component sender, object data)
    {
        if (pullHint != null)
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