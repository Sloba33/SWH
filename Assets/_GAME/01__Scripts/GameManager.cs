using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using Coherence.Connection;
using Coherence.Toolkit;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

public class GameManager : MonoBehaviour
{

    public GameObject skipButton;
    bool DeveloperMode;
    public bool IsMultiplayer;
    public CharacterCollection characterCollection;
    public CharacterCollection multiplayerCharacterCollection;
    public bool Recording, DarkLevel;
    public GameObject playerDefaultPrefab;
    public CoherenceSync playerNetworkedPrefab;
    private CoherenceBridge coherenceBridge;
    public GameObject connectNetworkUI;
    public Transform playerSpawnPoint;
    public Transform opponentSpawnPoint;
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
    public List<Obstacle> ObstaclesToModify = new List<Obstacle>();
    public float ObstacleWeightModifier = 1f;
    public int defaultZoomValue = 2;
    public bool isFinalChapterLevel;
    public Obstacle[] obstaclesToFreeze;
    public CollectibleItemDatabase collectibleDatabase;
    public bool CollectibleSpawned;
    public bool ShouldHaveSkipButton;

    [Header("Multiplayer Pre-game")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject controlsGameObject;
    [SerializeField] private string matchmakingSceneName = "MatchmakingTest";

    private int _countdownSeconds;
    private bool _isFirstPlayer;
    private bool _countdownStarted;

    /// <summary>
    /// Invoked when local player's character is spawned, true is passed if local player is the first player.
    /// </summary>
    public event Action<bool> OnLocalPlayerSpawned;
    public Player LocalPlayer;
    
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                Debug.LogError("Game Manager is null");
            return _instance;
        }
    }


    public void FreezeObstacles()
    {
        obstaclesToFreeze = FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
        for (int i = 0; i < obstaclesToFreeze.Length; i++)
        {
            obstaclesToFreeze[i].isFrozen = true;
        }
    }

    public void DisconnectAndReturnToMatchmaking()
    {
        if (coherenceBridge != null && coherenceBridge.IsConnected)
            coherenceBridge.Disconnect();
        SceneManager.LoadScene(matchmakingSceneName);
    }
    private void SetTestBuildPrefs()
    {
        if (PlayerPrefs.GetInt("Level") < 3)
        {
            PlayerPrefs.SetInt("FirstTime", 1);
            PlayerPrefs.SetInt("GameplayTutorialCompleted", 1);
            PlayerPrefs.SetInt("IntroMenuTutorialStage", 1);
            PlayerPrefs.SetInt("Level", 3);
        }

    }
    private void Awake()
    {
        // SetTestBuildPrefs();
        Debug.Log("Framerate set");
        Application.targetFrameRate = 60;
        collectibleDatabase = Resources.Load<CollectibleItemDatabase>("CollectibleItemsDatabase");
        _instance = this;
    }
    private void GenerateSkipButton()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameObject buttonObj = new GameObject("DEBUG BUTTON");
        buttonObj.transform.SetParent(canvas.transform, false);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.9f, 0.2f, 0.2f, 0.95f); // Red-ish semi-transparent

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        // Add Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = "SKIP";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // Position: 500px from Top-Left
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(500, -50); // 500px right, 50px down from top-left

        rect.sizeDelta = new Vector2(160, 60);

        // Make text fill the button
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Optional: Add action (e.g. restart level)
        button.onClick.AddListener(() =>
        {
            Debug.Log("<color=red>DEBUG BUTTON PRESSED!</color>");
            SkipLevel();
            // Example: UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
        skipButton = buttonObj;
        skipButton.SetActive(true);

    }

    private void SkipLevel()
    {
        LevelGoal levelGoal = FindObjectOfType<LevelGoal>();
        for (int i = 0; i < levelGoal.ObstaclesToDestroy_Player.Count; i++)
        {
            levelGoal.ObstaclesToDestroy_Player[i].ParticleDestroy();
        }
        skipButton.gameObject.SetActive(false);
    }
    public bool Testing;
    private void Start()
    {

        if (Application.isMobilePlatform)
        {
            int wid = Screen.width;
            int hei = Screen.height;
            QualitySettings.vSyncCount = 0;
            Screen.SetResolution(wid, hei, FullScreenMode.ExclusiveFullScreen, new RefreshRate() { numerator = 60, denominator = 1 });
        }
        start = true;

        levelGoal = FindFirstObjectByType<LevelGoal>();
        if(IsMultiplayer)
        {
            // connectNetworkUI.gameObject.SetActive(true);
            if (CoherenceBridgeStore.TryGetBridge(gameObject.scene, out coherenceBridge))
            {
                Debug.Log("[MP] Bridge found");
                coherenceBridge.onConnected.AddListener(OnMultiplayerConnected);
                coherenceBridge.onConnectionError.AddListener(OnMultiplayerConnectionError);
                coherenceBridge.onDisconnected.AddListener(OnMultiplayerDisconnected);
                
                coherenceBridge.ClientConnections.OnCreated += OnClientConnectionCreated;
                coherenceBridge.ClientConnections.OnDestroyed += OnClientConnectionDestroyed;
                coherenceBridge.ClientConnections.OnSynced += OnClientConnectionsSynced;
                
                coherenceBridge.JoinRoom(MatchmakingTestUI.MatchResult.Room);
            }
        }
        else
        {
            if(connectNetworkUI != null)
            {
                connectNetworkUI.gameObject.SetActive(false);
            }
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
        ObstaclesToModify = new List<Obstacle>(FindObjectsOfType<Obstacle>());
        for (int i = 0; i < ObstaclesToModify.Count; i++)
        {
            ObstaclesToModify[i].Weight *= ObstacleWeightModifier;

        }
        if (ShouldHaveSkipButton)
            GenerateSkipButton();
    }

    private void OnClientConnectionsSynced(CoherenceClientConnectionManager manager)
    {
        var others = manager.GetOtherClients();
        ClientID otherClientId = default;
        bool opponentAlreadyConnected = false;

        foreach(var client in others)
        {
            if(opponentAlreadyConnected)
            {
                Debug.LogError("[MP] More than 2 players in the server!");
            }

            otherClientId = client.ClientId;
            opponentAlreadyConnected = true;
        }

        _isFirstPlayer = !opponentAlreadyConnected;
        bool isHost = MatchmakingTestUI.MatchResult.IsHost;

        Transform spawnPoint = isHost ? playerSpawnPoint : opponentSpawnPoint;
        CoherenceSync sync = Instantiate(playerNetworkedPrefab, spawnPoint.position, spawnPoint.rotation);
        LocalPlayer = sync.GetComponent<Player>();
        OnLocalPlayerSpawned?.Invoke(isHost);

        if (_isFirstPlayer)
        {
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = "Waiting for opponent...";
            }
        }
        else
        {
            // Player 2: opponent already present, start countdown immediately
            StartCoroutine(RunCountdown());
        }
    }

    private void OnClientConnectionDestroyed(CoherenceClientConnection connection)
    {
        if(coherenceBridge.ClientConnections.GetMine() != connection)
        {
            Debug.LogWarning("[MP] Opponent connection destroyed");
        }
        else
        {
            Debug.LogWarning("[MP] My own connection destroyed");
        }
    }

    private void OnClientConnectionCreated(CoherenceClientConnection newConnection)
    {
        if(coherenceBridge.ClientConnections.GetMine() != newConnection)
        {
            Debug.LogWarning("[MP] Opponent connection created");
            if (_isFirstPlayer && !_countdownStarted)
                StartCoroutine(RunCountdown());
        }
        else
        {
            Debug.LogWarning("[MP] My own connection created, ignoring");
        }
    }

    private void OnMultiplayerDisconnected(CoherenceBridge arg0, ConnectionCloseReason arg1)
    {
        Debug.Log("[MP] Disconnected");
    }

    private void OnMultiplayerConnectionError(CoherenceBridge arg0, ConnectionException arg1)
    {
        Debug.Log("[MP] ConnectionError");
    }

    private void OnMultiplayerConnected(CoherenceBridge arg0)
    {
        Debug.Log("[MP] Connected");
    }

    public float fragScaleFactor = 1;

    private IEnumerator RunCountdown()
    {
        _countdownStarted = true;
        _countdownSeconds = 3;

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        while (_countdownSeconds > 0)
        {
            if (countdownText != null)
                countdownText.text = _countdownSeconds.ToString();
            yield return new WaitForSeconds(1f);
            _countdownSeconds--;
        }

        if (countdownText != null)
        {
            countdownText.text = "GO!";
            yield return new WaitForSeconds(0.8f);
            countdownText.gameObject.SetActive(false);
        }

        if (controlsGameObject != null)
            controlsGameObject.SetActive(true);
    }

}
