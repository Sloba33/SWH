using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;



using Cinemachine;
using Fusion;
public class GameManager : MonoBehaviour
{
    public bool IsMultiplayer;
    public CharacterCollection characterCollection;
    public CharacterCollection multiplayerCharacterCollection;
    public bool Recording, DarkLevel;
    public GameObject playerDefaultPrefab;
    public Transform playerSpawnPoint;
    public LevelGoal levelGoal;
    public bool jitbSpawned;
    public List<Obstacle> blackHoleObstacles = new();
    public List<Obstacle> jitbObstacles = new();
    public GameObject blackHolePrefab;
    public bool blackHole;
    private static GameManager _instance;
    public GameObject obstacleToSpawn;
    public bool start, spawn;
    public GoalSetter[] goalSetters;
    public GoalSetter playerGoalSetter, AIGoalSetter;
    public bool ShouldHaveMainMenuButton;
    public bool SendsBackToMainMenu;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                Debug.LogError("Game Manager is null");
            return _instance;
        }
    }
    private void Awake()
    {
        Debug.Log("Framerate set");
        Application.targetFrameRate = 60;
        _instance = this;

    }
    private void Start()
    {
        IsMultiplayer = FindFirstObjectByType<NetworkRunner>() != null;
        if (Application.isMobilePlatform)
        {
            int wid = Screen.width;
            int hei = Screen.height;
            QualitySettings.vSyncCount = 0;
            Screen.SetResolution(wid, hei, FullScreenMode.ExclusiveFullScreen, new RefreshRate() { numerator = 60, denominator = 1 });
        }
        start = true;

        levelGoal = FindFirstObjectByType<LevelGoal>();
        // if (IsMultiplayer && multiplayerCharacterCollection != null)
        // {
        //     Instantiate(multiplayerCharacterCollection.Characters[0], playerSpawnPoint.position, multiplayerCharacterCollection.Characters[0].transform.rotation);
        // }
        if (!IsMultiplayer)
        {

            if (characterCollection != null)
            {
                Instantiate(characterCollection.Characters[PlayerPrefs.GetInt("SelectedCharacterID", 0)], playerSpawnPoint.position, characterCollection.Characters[PlayerPrefs.GetInt("SelectedCharacterID", 0)].transform.rotation);
            }
            else if (playerDefaultPrefab != null && !playerDefaultPrefab.GetComponent<PlayerController>().AI)
            {
                Debug.Log("We got no AI");
                Instantiate(playerDefaultPrefab, playerSpawnPoint.position, playerDefaultPrefab.transform.rotation);
            }
        }
        goalSetters = FindObjectsByType<GoalSetter>(FindObjectsSortMode.None);
        if (levelGoal.DualLevel)
            if (goalSetters[0].AIGoal)
            {
                AIGoalSetter = goalSetters[0];
                playerGoalSetter = goalSetters[1];
            }
            else
            {
                AIGoalSetter = goalSetters[1];
                playerGoalSetter = goalSetters[0];
            }

    }
    public float fragScaleFactor = 1;




}
