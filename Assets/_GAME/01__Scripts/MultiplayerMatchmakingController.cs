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
/// wires the cancel button, and falls back to a local bot match (a state-replay
/// ghost, see <see cref="StateReplay"/>) if no opponent shows up within the
/// configured timeout.
///
/// The scene is self-sufficient: it can be entered directly in the editor (or be
/// the first scene of a test build) without going through boot/menu scenes — all
/// inputs come from PlayerPrefs and serialized fields. For fast iteration,
/// <see cref="debugInstantBotMatch"/> skips Coherence entirely and starts a bot
/// match immediately.
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
    [Tooltip("Resources sub-folder that holds the StateReplay assets. On fallback one valid replay is " +
             "picked at random and its scene is loaded as a local bot match.")]
    [SerializeField] private string botReplaysResourcesFolder = "BotReplays";

    [Header("Testing")]
    [Tooltip("Skip Coherence matchmaking entirely and start a bot match immediately with a random replay. " +
             "For iterating from this scene without waiting out the fallback timeout. Leave OFF in production.")]
    [SerializeField] private bool debugInstantBotMatch;

    [Header("Navigation")]
    [SerializeField] private SceneReference mainMenuScene;
    [Tooltip("Seconds to keep the cancel/error message on screen before returning to the main menu.")]
    [SerializeField] private float exitDelaySeconds = 0.4f;

    [Header("Status Labels")]
    [SerializeField] private string waitingText = "Waiting for opponent...";
    [SerializeField] private string opponentFoundText = "Opponent found!";
    [SerializeField] private string gameStartingText = "Game starting...";
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

        if (debugInstantBotMatch && TryStartInstantBotMatch())
        {
            return;
        }

        StartMatchmaking();
    }

    /// <summary>
    /// Testing shortcut: start a bot match right now, no Coherence involved.
    /// Returns false (falling through to normal matchmaking) when no usable
    /// replay exists.
    /// </summary>
    private bool TryStartInstantBotMatch()
    {
        StateReplay replay = PickRandomBotReplay();
        if (replay == null)
        {
            Debug.LogWarning("[Matchmaking] debugInstantBotMatch is on but no usable replay was found — running normal matchmaking.");
            return false;
        }
        Debug.Log($"[Matchmaking] debugInstantBotMatch: starting bot match with '{replay.name}' in '{replay.scene.Name}'.");
        SetStatus(gameStartingText);
        SetCancelInteractable(false);
        BotMatchContext.PendingReplay = replay;
        SceneManager.LoadScene(replay.scene.Name);
        return true;
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
            // replay attached. If no usable replay exists, fallback is effectively
            // off and matchmaking simply keeps waiting for a real opponent.
            StateReplay botCandidate = PickRandomBotReplay(); // validated: scene is loadable
            var botLevelNames = new List<string>();
            if (botCandidate != null)
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
                // Dev-facing log only — the on-screen status shows the same messages
                // as a real match so players can't tell the difference.
                Debug.Log($"[Matchmaking] BOT match: replay '{botCandidate.name}' ({botCandidate.label}) in '{result.BotLevelScene}'.");

                // Fake the real handshake's pacing: a genuine match has network
                // latency between "Opponent found!" (already showing, set by the
                // BotFallback state) and "Game starting..." (session start round
                // trip), then a short wait for room data. Without this the bot
                // path flips both messages instantly — a tell.
                await Task.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(1.0f, 2.2f)), token);
                SetStatus(gameStartingText);
                await Task.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(0.3f, 0.8f)), token);

                BotMatchContext.PendingReplay = botCandidate;
                SceneManager.LoadScene(result.BotLevelScene);
            }
            else
            {
                Debug.Log($"[Matchmaking] REAL multiplayer match in '{result.MapName}' (host: {result.IsHost}).");
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
                // Players must not be able to tell a bot match from a real one:
                // show the same message a real match shows at this point. The
                // console log in RunMatchmakingAsync keeps the distinction for us.
                SetStatus(opponentFoundText);
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
    /// random one whose scene is loadable (assigned + in Build Settings), or null
    /// when fallback is disabled or nothing usable exists. Invalid assets are
    /// reported individually instead of silently disabling the fallback. Must be
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

        var valid = new List<StateReplay>(replays.Length);
        foreach (StateReplay replay in replays)
        {
            if (replay.scene != null && replay.scene.UnsafeReason == SceneReferenceUnsafeReason.None)
                valid.Add(replay);
            else
                Debug.LogWarning($"[Matchmaking] Replay '{replay.name}' has no loadable scene " +
                                 $"({(replay.scene == null ? "unassigned" : replay.scene.UnsafeReason.ToString())}) — skipped.", replay);
        }

        if (valid.Count == 0)
        {
            Debug.LogWarning("[Matchmaking] No replay with a loadable scene — bot fallback unavailable.");
            return null;
        }
        return valid[UnityEngine.Random.Range(0, valid.Count)];
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
