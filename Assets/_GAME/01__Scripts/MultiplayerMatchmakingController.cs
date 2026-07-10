using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eflatun.SceneReference;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives <see cref="CoherenceMatchmaker"/> for the production matchmaking-waiting
/// scene. Shows a "Waiting for opponent / Opponent found / Game starting" status,
/// wires the cancel button, and falls back to a bot scene if no opponent shows up
/// within the configured timeout.
/// </summary>
public class MultiplayerMatchmakingController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button cancelButton;

    [Header("Match Parameters")]
    [Tooltip("Leave empty to allow matching on any map. In that case a random scene from " +
             "Multiplayer Level Scenes is used if we end up creating the lobby.")]
    [SerializeField] private string mapName = "";
    [SerializeField] private string region = "eu";

    // Populated by the main menu before loading the matchmaking scene. Static so the
    // values survive the scene transition without needing a DontDestroyOnLoad carrier.
    // Defaults match the previous serialized defaults so the controller still works
    // if someone forgets to set them (e.g. when entering the scene directly in-editor).
    public static int Skill = 100;
    public static int MinOpponentSkill = 0;
    public static int MaxOpponentSkill = 1000;

    [Header("Map Pools")]
    [Tooltip("Scenes considered valid for multiplayer. Used only when matchmaking with an " +
             "empty map name and we end up creating the lobby — one is picked at random.")]
    [SerializeField] private List<SceneReference> multiplayerLevelScenes = new();

    [Header("Bot Fallback")]
    [SerializeField] private bool fallbackToBotEnabled = true;
    [SerializeField] private float botFallbackTimeoutSeconds = 15f;
    [Tooltip("Resources sub-folder that holds the BotReplay assets. On fallback one is picked at " +
             "random and its scene is loaded as a local bot match.")]
    [SerializeField] private string botReplaysResourcesFolder = "BotReplays";

    [Header("Navigation")]
    [SerializeField] private SceneReference mainMenuScene;
    [Tooltip("Seconds to keep the cancel/error message on screen before returning to the main menu.")]
    [SerializeField] private float exitDelaySeconds = 0.4f;

    [Header("Status Labels")]
    [SerializeField] private string waitingText = "Waiting for opponent...";
    [SerializeField] private string opponentFoundText = "Opponent found!";
    [SerializeField] private string gameStartingText = "Game starting...";
    [SerializeField] private string botFallbackText = "No opponent found. Starting bot match...";
    [SerializeField] private string cancelledText = "Cancelling...";
    [SerializeField] private string failedText = "Matchmaking failed.";

    private CancellationTokenSource _cts;
    private bool _isExiting;

    [SerializeField]
    private float opponentSkillBand = 100;

    private void Awake()
    {
        int trophies = TrophyUtility.GetDisplayedTrophies();
        trophies = Mathf.Max(trophies, 0); // Ensure trophies is not negative
        int minTrophies = (int)Mathf.Max(trophies - opponentSkillBand, 0);
        int maxTrophies = (int)(trophies + opponentSkillBand);

        Skill = trophies;
        MinOpponentSkill = minTrophies;
        MaxOpponentSkill = maxTrophies;
    }

    private void Start()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
            cancelButton.interactable = true;
        }

        CoherenceMatchmaker.StateChanged += OnMatchmakingStateChanged;
        ApplyState(CoherenceMatchmaker.MatchmakingState.WaitingForOpponent);

        StartMatchmaking();
    }

    private void OnDestroy()
    {
        CoherenceMatchmaker.StateChanged -= OnMatchmakingStateChanged;
        if (_cts != null)
        {
            try { _cts.Cancel(); } catch { }
            _cts.Dispose();
            _cts = null;
        }
    }

    private void StartMatchmaking()
    {
        if (_cts != null)
        {
            return;
        }
        _cts = new CancellationTokenSource();
        _ = RunMatchmakingAsync(_cts.Token);
    }

    private async Task RunMatchmakingAsync(CancellationToken token)
    {
        try
        {
            // Pick the bot replay up front (on the main thread, before any await).
            // We hand the matchmaker just this replay's scene as the bot-level pool
            // so its fallback path resolves to the same scene we then load with the
            // replay attached. If no replays exist, fallback is effectively off and
            // matchmaking simply keeps waiting for a real opponent.
            StateReplay botCandidate = PickRandomBotReplay();
            var botLevelNames = new List<string>();
            if (botCandidate != null && botCandidate.scene != null &&
                botCandidate.scene.UnsafeReason == SceneReferenceUnsafeReason.None)
            {
                botLevelNames.Add(botCandidate.scene.Name);
            }

            var result = await CoherenceMatchmaker.FindMatchAsync(
                mapName: mapName,
                skillLevel: Skill,
                minOpponentSkill: MinOpponentSkill,
                maxOpponentSkill: MaxOpponentSkill,
                region: region,
                multiplayerLevels: ToSceneNames(multiplayerLevelScenes),
                botLevels: botLevelNames,
                fallbackToBotEnabled: fallbackToBotEnabled,
                botFallbackTimeout: TimeSpan.FromSeconds(botFallbackTimeoutSeconds),
                onProgress: msg => Debug.Log("[Matchmaking] " + msg),
                cancellationToken: token);

            if (result.IsBotFallback)
            {
                // Hand the chosen replay to the level scene, then load it. GameManager
                // reads BotMatchContext to run the match locally against this bot.
                BotMatchContext.PendingReplay = botCandidate;
                SceneManager.LoadScene(result.BotLevelScene);
            }
            else
            {
                BotMatchContext.Clear();
                // Use the resolved MapName from the result — when matchmaking any-map,
                // the local `mapName` field is empty and the real scene comes from the
                // lobby attribute.
                SceneManager.LoadScene(result.MapName);
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus(cancelledText);
            await ReturnToMainMenuAfterDelayAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetStatus(failedText);
            await ReturnToMainMenuAfterDelayAsync();
        }
        finally
        {
            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }
        }
    }

    private async Task ReturnToMainMenuAfterDelayAsync()
    {
        if (_isExiting)
        {
            return;
        }
        _isExiting = true;
        SetCancelInteractable(false);
        if (exitDelaySeconds > 0f)
        {
            await Task.Delay(TimeSpan.FromSeconds(exitDelaySeconds));
        }
        if (this == null)
        {
            return;
        }
        SceneManager.LoadScene(mainMenuScene.Name);
    }

    private void OnCancelClicked()
    {
        if (!CoherenceMatchmaker.CanCancel)
        {
            return;
        }
        CoherenceMatchmaker.TryCancel();
    }

    private void OnMatchmakingStateChanged(CoherenceMatchmaker.MatchmakingState state)
    {
        ApplyState(state);
    }

    private void ApplyState(CoherenceMatchmaker.MatchmakingState state)
    {
        switch (state)
        {
            case CoherenceMatchmaker.MatchmakingState.WaitingForOpponent:
                SetStatus(waitingText);
                SetCancelInteractable(true);
                break;
            case CoherenceMatchmaker.MatchmakingState.OpponentFound:
                SetStatus(opponentFoundText);
                SetCancelInteractable(false);
                break;
            case CoherenceMatchmaker.MatchmakingState.GameStarting:
                SetStatus(gameStartingText);
                SetCancelInteractable(false);
                break;
            case CoherenceMatchmaker.MatchmakingState.BotFallback:
                SetStatus(botFallbackText);
                SetCancelInteractable(false);
                break;
            case CoherenceMatchmaker.MatchmakingState.Cancelled:
                SetStatus(cancelledText);
                SetCancelInteractable(false);
                break;
            case CoherenceMatchmaker.MatchmakingState.Failed:
                SetStatus(failedText);
                SetCancelInteractable(false);
                break;
        }
    }

    private void SetCancelInteractable(bool enabled)
    {
        if (cancelButton != null)
        {
            cancelButton.interactable = enabled;
        }
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    /// <summary>
    /// Loads every StateReplay from the configured Resources folder and returns a
    /// random one, or null when fallback is disabled or no replays exist. Must be
    /// called on the main thread (Resources.LoadAll is not thread-safe).
    /// </summary>
    private StateReplay PickRandomBotReplay()
    {
        if (!fallbackToBotEnabled)
        {
            return null;
        }
        StateReplay[] replays = Resources.LoadAll<StateReplay>(botReplaysResourcesFolder);
        if (replays == null || replays.Length == 0)
        {
            Debug.LogWarning($"[Matchmaking] Bot fallback is enabled but no StateReplay assets were found in " +
                             $"Resources/{botReplaysResourcesFolder}. Matchmaking will keep waiting for a real opponent.");
            return null;
        }
        return replays[UnityEngine.Random.Range(0, replays.Length)];
    }

    private static List<string> ToSceneNames(List<SceneReference> refs)
    {
        var names = new List<string>(refs.Count);
        foreach (var sceneRef in refs)
        {
            if (sceneRef != null && sceneRef.UnsafeReason == SceneReferenceUnsafeReason.None)
            {
                names.Add(sceneRef.Name);
            }
        }
        return names;
    }
}
