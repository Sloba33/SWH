#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only driver of a replay take session (see Tools/SWH/Replay/Record
/// Replay In This Scene). Lifecycle:
///   1. Scene starts frozen (GameManager.PreMatchFreeze) with a local human.
///   2. The player's first action (move/jump/hit/pull/special) unfreezes the
///      level and starts the recorder in the same frame — so replay t=0 is the
///      first action, exactly matching a match's countdown-end playback start.
///   3. The run ending (win, or death via any path) stops the recording after a
///      short tail (to capture settles/death) and opens the save dialog,
///      defaulting to Assets/_GAME/04_Data. Play mode exits after saving.
/// </summary>
public class ReplayTakeController : MonoBehaviour
{
    public const string SaveFolder = "Assets/_GAME/04_Data";

    private StateReplayRecorder _recorder;
    private Player _player;
    private PlayerInputHandler _input;
    private Settings _settings;
    private bool _started;
    private bool _ending;

    // Captured at recording start (after Player.Start applied override/upgrade
    // stats) and stamped into the saved replay's label for later filtering.
    private float _takeMoveSpeed;
    private float _takeStrength;

    public void Initialize(Player player, Transform levelRoot)
    {
        _player = player;
        _input = player != null ? player.GetComponent<PlayerInputHandler>() : null;

        _recorder = gameObject.AddComponent<StateReplayRecorder>();
        _recorder.target = player;
        _recorder.levelRoot = levelRoot;
        if (levelRoot == null)
            Debug.LogWarning("[ReplayTake] LevelGoal has no player level root — recording in world space, " +
                             "entity events disabled. Check the scene's LevelGoal.playerLevel.");

        Player.PlayerDied += OnPlayerDied;
        Debug.Log("[ReplayTake] Ready. The level is frozen — make any move to start recording.");
    }

    private void OnDestroy()
    {
        Player.PlayerDied -= OnPlayerDied;
    }

    private void Update()
    {
        if (!_started)
        {
            if (_input != null && FirstActionDetected())
            {
                _started = true;
                _takeMoveSpeed = _player != null ? _player.StartingMoveSpeed : 0f;
                _takeStrength = _player != null ? _player.StartingStrenght : 0f;
                GameManager.Instance.SetPreMatchFreeze(false);
                _recorder.StartRecording();
                Debug.Log("[ReplayTake] First action — level unfrozen, recording started.");
            }
            return;
        }

        if (_ending) return;

        // Win/lose both surface through the shared Settings flags.
        if (_settings == null) _settings = FindObjectOfType<Settings>();
        if (_settings != null && (_settings.gameWon || _settings.gameLost))
            EndTake(_settings.gameWon ? "win" : "loss", 1.0f);
    }

    private bool FirstActionDetected()
    {
        return _input.MoveInput != Vector2.zero
               || _input.GetJumpPressedThisFrame()
               || _input.GetHitPressedThisFrame()
               || _input.GetHitDownPressedThisFrame()
               || _input.GetSpecialAttackPressedThisFrame()
               || _input.GetPullPressedThisFrame();
    }

    private void OnPlayerDied(Player who)
    {
        if (who != _player) return;
        if (!_started)
        {
            // Died before acting (fell at spawn?) — nothing worth saving.
            Debug.LogWarning("[ReplayTake] Player died before the take started; nothing recorded.");
            return;
        }
        EndTake("death", 1.5f); // tail long enough for the death event + corpse settle
    }

    private void EndTake(string outcome, float tailSeconds)
    {
        if (_ending) return;
        _ending = true;
        StartCoroutine(EndTakeRoutine(outcome, tailSeconds));
    }

    private IEnumerator EndTakeRoutine(string outcome, float tailSeconds)
    {
        Debug.Log($"[ReplayTake] Run ended ({outcome}) — capturing {tailSeconds:F1}s tail…");
        yield return new WaitForSeconds(tailSeconds);
        _recorder.StopRecording();
        SaveTake(outcome);
    }

    private void SaveTake(string outcome)
    {
        EnsureSaveFolder();

        string sceneName = SceneManager.GetActiveScene().name;
        string defaultName = $"Replay_{sceneName}_{outcome}";
        string path = EditorUtility.SaveFilePanelInProject(
            "Save State Replay", defaultName, "asset",
            "Save the recorded replay. (Move/copy it into a Resources/BotReplays folder to use it for matchmaking bot fallback.)",
            SaveFolder);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[ReplayTake] Save cancelled — the take was discarded.");
        }
        else
        {
            StateReplay replay = ScriptableObject.CreateInstance<StateReplay>();
            _recorder.PopulateReplay(replay);
            // Parseable key=value label for filtering replays later (invariant
            // culture so decimals are always dots regardless of editor locale).
            replay.label = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0} speed={1:0.##} strength={2:0.##} duration={3}",
                outcome, _takeMoveSpeed, _takeStrength, Mathf.RoundToInt(replay.duration));
            AssetDatabase.CreateAsset(replay, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(replay);
            Debug.Log($"[ReplayTake] Saved replay to {path} — label: '{replay.label}'.");
        }

        EditorApplication.isPlaying = false;
    }

    private static void EnsureSaveFolder()
    {
        if (!AssetDatabase.IsValidFolder(SaveFolder))
            AssetDatabase.CreateFolder("Assets/_GAME", "04_Data");
    }
}
#endif
