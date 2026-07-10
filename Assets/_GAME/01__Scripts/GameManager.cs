using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using Coherence.Connection;
using Coherence.Toolkit;
using Eflatun.SceneReference;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

public class GameManager : MonoBehaviour
{

    public GameObject skipButton;
    bool DeveloperMode;
    public bool IsMultiplayer;
    /// <summary>
    /// True for a local bot match: the level is a multiplayer scene, but we run it
    /// locally with no Coherence connection against a replay-driven bot. Set in
    /// Awake (before any Start) so LevelGoal and the networked-command guards can
    /// rely on it. IsMultiplayer stays true so the MP win/lose screens are used.
    /// </summary>
    public bool IsBotMatch { get; private set; }
    /// <summary>
    /// True from match setup until gameplay begins. While set, live obstacles
    /// hold still (no scripted falling) so both sides' falling schedules start
    /// together. Cleared: at countdown end (bot matches and real multiplayer),
    /// or at the player's first action (replay take sessions).
    /// </summary>
    public bool PreMatchFreeze { get; private set; }

    /// <summary>
    /// Single entry point for freeze transitions — rockets pause their idle spin
    /// while frozen and restart it from phase 0 at gameplay start, keeping their
    /// spin phase aligned between recording sessions and match playback.
    /// </summary>
    public void SetPreMatchFreeze(bool value)
    {
        if (PreMatchFreeze == value) return;
        PreMatchFreeze = value;
        Rocket.OnPreMatchFreezeChanged(value);
    }

    /// <summary>
    /// Editor-only recording session started via Tools/SWH/Replay: the scene runs
    /// locally (no Coherence), a local human is spawned, and ReplayTakeController
    /// records the run. Always false in builds.
    /// </summary>
    public bool IsReplayTakeSession { get; private set; }

    private StateReplay _pendingBotReplay;
    private StateReplayDriver _botGhostDriver;
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
    public float ObstacleWeightModifier = 1f;
    public int defaultZoomValue = 2;
    public bool isFinalChapterLevel;
    public Obstacle[] obstaclesToFreeze;
    public CollectibleItemDatabase collectibleDatabase;
    public bool CollectibleSpawned;
    public bool ShouldHaveSkipButton = true;

    [Header("Multiplayer Pre-game")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject controlsGameObject;
    
    private int _countdownSeconds;
    private bool _isFirstPlayer;
    private bool _countdownStarted;
    // Set when the local player initiates a disconnect (Main Menu / quit). Tells
    // OnMultiplayerDisconnected to skip the forfeit LoseScreen, since we're
    // already scene-loading back to matchmaking. Involuntary disconnects leave
    // this false and get the LoseScreen.
    private bool _voluntaryDisconnect;

    /// <summary>
    /// Invoked when local player's character is spawned, true is passed if local player is the first player.
    /// </summary>
    public event Action<bool> OnLocalPlayerSpawned;
    public Player LocalPlayer;

    /// <summary>
    /// The player this client controls. In multiplayer both characters are Player
    /// instances, so a scene-wide find can return the opponent — always resolve
    /// through here instead of FindObjectOfType&lt;Player&gt;(). LocalPlayer is only
    /// assigned on the multiplayer spawn path; in single player we fall back to a
    /// scene find (there is exactly one Player). May return null in multiplayer
    /// before the local character has spawned.
    /// </summary>
    public Player GetLocalPlayer()
    {
        if (LocalPlayer == null && !IsMultiplayer)
            LocalPlayer = FindFirstObjectByType<Player>();
        return LocalPlayer;
    }
    
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
        Disconnect();
        FindAnyObjectByType<SceneLoader>().LoadSceneFile("03_Matchmaking");
    }

    private void Disconnect()
    {
        _voluntaryDisconnect = true;
        if (coherenceBridge != null && coherenceBridge.IsConnected)
            coherenceBridge.Disconnect();

        // Leave the matchmaking lobby so we don't hold a membership in the
        // background. Coherence caps a player at 3 concurrent lobbies, so
        // leaking one per game would lock us out after a few matches.
        // LobbySession lives on the PlayerAccount, not on this scene, so the
        // request safely outlives the scene change below.
        _ = CoherenceMatchmaker.LeaveLobbyAsync(CoherenceMatchmaker.LatestMatch.Lobby);
        CoherenceMatchmaker.ClearLatestMatch();
    }

    public void DisconnectAndReturnToMainMenu()
    {
        Disconnect();
        FindAnyObjectByType<SceneLoader>().LoadSceneFile("01_MainMenu");
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

        // Consume the bot-match handoff here (in Awake) so the flag is set before
        // any other component's Start reads it — notably LevelGoal, which picks
        // its init path based on it.
        IsBotMatch = BotMatchContext.IsBotMatch;
        _pendingBotReplay = BotMatchContext.PendingReplay;
        BotMatchContext.Clear();

#if UNITY_EDITOR
        // Editor-only: a replay take session flagged by the Tools/SWH/Replay menu
        // (SessionState survives the play-mode domain reload).
        IsReplayTakeSession = !IsBotMatch && ReplayTakeSession.IsActive;
#endif
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
        // Bot scenes are multiplayer scenes too, so IsMultiplayer is set in both
        // cases. IsBotMatch (resolved in Awake) is what tells us to run locally
        // (no Coherence) instead of connecting to a room; IsReplayTakeSession is
        // the editor-only recording flow, also fully local.
        bool isBotMatch = IsBotMatch;
        if(IsMultiplayer && !isBotMatch && !IsReplayTakeSession)
        {
            // Real multiplayer freezes obstacle simulation until the countdown
            // ends (cleared in RunCountdown), same as bot matches — no boxes
            // falling while the players can't move yet.
            SetPreMatchFreeze(true);
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
                
                coherenceBridge.JoinRoom(CoherenceMatchmaker.LatestMatch.Room);
            }
        }
        else if (isBotMatch)
        {
            StartBotMatch();
        }
        else if (IsReplayTakeSession)
        {
            StartReplayTakeSession();
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
        // ObstacleWeightModifier is applied per-obstacle in Obstacle.Start() (see Obstacle.EffectiveWeight),
        // so it covers both scene-placed and later-spawned falling obstacles. Cardboard is excluded there.
        if (ShouldHaveSkipButton)
            GenerateSkipButton();
    }

    /// <summary>
    /// Runs a local, non-networked match against a bot. The level is a normal
    /// multiplayer scene, but instead of connecting to Coherence we spawn the
    /// local human (their selected character) and a bot opponent driven by the
    /// chosen replay. To the player it should be indistinguishable from a real
    /// match, so we still run the pre-game countdown.
    /// </summary>
    private void StartBotMatch()
    {
        // Hold all live obstacle simulation until the countdown ends (cleared in
        // RunCountdown, right where the ghost starts playing).
        SetPreMatchFreeze(true);

        if (connectNetworkUI != null)
            connectNetworkUI.gameObject.SetActive(false);

        if (characterCollection == null || characterCollection.Characters.Count == 0)
        {
            Debug.LogError("[BotMatch] characterCollection is empty; cannot spawn bot match.");
            return;
        }

        // Local human player — their selected single-player character.
        LocalPlayer = SpawnLocalHumanCharacter();
        // Note: we deliberately do NOT fire OnLocalPlayerSpawned here. Its only
        // listener (LevelGoal) self-initializes for bot matches, and invoking it
        // synchronously from Start would race LevelGoal's own Start (it may not
        // have subscribed yet).

        // Bot opponent — a random single-player character turned into a state-replay
        // ghost: AttachGhost neutralizes it (gameplay components disabled, colliders
        // off, cameras destroyed) and the driver kinematically replays the recorded
        // run into the opponent half. Playback starts when the countdown finishes
        // (see RunCountdown), mirroring when the recorded human gained control.
        int botId = UnityEngine.Random.Range(0, characterCollection.Characters.Count);
        GameObject botPrefab = characterCollection.Characters[botId];
        GameObject bot = Instantiate(botPrefab, opponentSpawnPoint.position, botPrefab.transform.rotation);

        Transform opponentRoot = levelGoal != null ? levelGoal.OpponentLevelRoot : null;
        // The bot half is replay-driven only: freeze its live physics so authored
        // at-height obstacles don't free-fall on their own at match start.
        StateReplayDriver.FreezeReplayHalf(opponentRoot);
        _botGhostDriver = StateReplayDriver.AttachGhost(bot, _pendingBotReplay, opponentRoot, autoPlay: false);

        // The local human vs. the bot share one Settings/LevelGoal, so neither
        // player's own win/lose path can decide the match. The arbiter owns the
        // outcome and routes it through the existing MP win/lose flow.
        BotMatchArbiter arbiter = gameObject.AddComponent<BotMatchArbiter>();
        arbiter.Initialize(LocalPlayer, levelGoal, _botGhostDriver);

        // Both "players" are present from the start, so kick off the countdown
        // immediately (mirrors the player-2 path in OnClientConnectionsSynced).
        StartCoroutine(RunCountdown());
    }

    private Player SpawnLocalHumanCharacter()
    {
        int selectedId = Mathf.Clamp(PlayerPrefs.GetInt("SelectedCharacterID", 0), 0, characterCollection.Characters.Count - 1);
        GameObject humanPrefab = characterCollection.Characters[selectedId];
        GameObject human = Instantiate(humanPrefab, playerSpawnPoint.position, humanPrefab.transform.rotation);
        return human.GetComponent<Player>();
    }

    /// <summary>
    /// Editor-only recording flow (Tools/SWH/Replay/Record Replay In This Scene):
    /// runs the multiplayer scene fully locally with just the human, frozen until
    /// their first action. ReplayTakeController owns the record/unfreeze/save
    /// lifecycle. No countdown, no bot, no arbiter — this is a take, not a match.
    /// </summary>
    private void StartReplayTakeSession()
    {
#if UNITY_EDITOR
        SetPreMatchFreeze(true);

        if (connectNetworkUI != null)
            connectNetworkUI.gameObject.SetActive(false);

        if (characterCollection == null || characterCollection.Characters.Count == 0)
        {
            Debug.LogError("[ReplayTake] characterCollection is empty; cannot start a take session.");
            return;
        }

        LocalPlayer = SpawnLocalHumanCharacter();

        if (controlsGameObject != null)
            controlsGameObject.SetActive(true);

        GameObject takeGO = new GameObject("ReplayTakeController");
        ReplayTakeController take = takeGO.AddComponent<ReplayTakeController>();
        take.Initialize(LocalPlayer, levelGoal != null ? levelGoal.PlayerLevelRoot : null);
#endif
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
        bool isHost = CoherenceMatchmaker.LatestMatch.IsHost;

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
            TryForfeitWin();
        }
        else
        {
            Debug.LogWarning("[MP] My own connection destroyed");
        }
    }

    // Opponent vanished mid-match (voluntary quit or network drop). We treat any
    // disconnect as a forfeit: the surviving player wins via the existing
    // WinLevel flow. No grace period — Coherence's connection-destroyed fires
    // after its own timeout, which is already the soak time.
    //
    // Two non-obvious bits:
    // 1. We steal authority over LevelGoal. If the disconnected opponent owned
    //    it, Coherence will otherwise disable its GameObject on this proxy.
    // 2. We host the coroutine on GameManager, not on LevelGoal, so the
    //    coroutine survives any momentary disable between disconnect-detection
    //    and authority-grant.
    private void TryForfeitWin()
    {
        if (levelGoal == null) return;
        Settings settings = FindObjectOfType<Settings>();
        if (settings != null && (settings.gameWon || settings.gameLost)) return;
        levelGoal.TakeAuthority();
        StartCoroutine(levelGoal.WinLevel(0.9f));
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
        if (_voluntaryDisconnect) return;
        TryForfeitLose();
    }

    // Local player got dropped (network failure, server kick). Treat it as a
    // forfeit on our side too — LoseScreen, symmetric with the opponent's
    // WinScreen. Voluntary leaves skip this via the _voluntaryDisconnect flag
    // because they're already scene-loading back to matchmaking.
    private void TryForfeitLose()
    {
        if (levelGoal == null) return;
        Settings settings = FindObjectOfType<Settings>();
        if (settings != null && (settings.gameWon || settings.gameLost)) return;
        // Hosted on GameManager (not LevelGoal) for the same reason as
        // TryForfeitWin — our bridge is down, so we can't rely on LevelGoal
        // staying enabled.
        StartCoroutine(levelGoal.LoseLevel());
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

        // Gameplay begins — live obstacles may fall and rocket spins restart from
        // phase 0. In a bot match the ghost starts at the same instant, matching
        // the recording's t=0 (driver null in real multiplayer).
        SetPreMatchFreeze(false);
        if (_botGhostDriver != null)
            _botGhostDriver.Play();
    }

}
