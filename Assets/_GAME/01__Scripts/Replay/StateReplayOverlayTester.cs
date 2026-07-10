using UnityEngine;

/// <summary>
/// Debug harness for checking state-replay fidelity: record a run, then spawn a
/// ghost that replays it overlaid on the live level while you keep playing. If
/// the ghost retraces the recorded run exactly (same corners, same animation
/// beats), the record/playback pair is faithful.
///
/// Drop on any GameObject in a level scene:
///   F9  — start / stop recording the local player.
///   F10 — spawn the ghost and play the last recording (replaces a running ghost).
///
/// The ghost plays back in the SAME level root it was recorded in, so it overlays
/// the human's half — deliberately, for side-by-side comparison. Real bot matches
/// will play into the opponent half instead.
/// </summary>
public class StateReplayOverlayTester : MonoBehaviour
{
    [Tooltip("Recorder to drive. Auto-added to this GameObject if empty.")]
    public StateReplayRecorder recorder;

    [Tooltip("Saved StateReplay asset to play instead of the last in-memory recording. Assign this to test a " +
             "take on a FRESH run (restart play mode after saving), so the level is in its start state rather " +
             "than the end state your recording session left behind.")]
    public StateReplay replayAsset;

    [Tooltip("Optional clean character prefab for the ghost. If empty, the live player GameObject is cloned " +
             "(works, but Awake-spawned attachments like the helmet get duplicated on the clone).")]
    public GameObject ghostPrefab;

    public KeyCode recordKey = KeyCode.F9;
    public KeyCode playKey = KeyCode.F10;

    private StateReplay _lastRecording;
    private GameObject _ghost;

    private void Awake()
    {
        if (recorder == null)
        {
            recorder = GetComponent<StateReplayRecorder>();
            if (recorder == null) recorder = gameObject.AddComponent<StateReplayRecorder>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(recordKey)) ToggleRecording();
        if (Input.GetKeyDown(playKey)) PlayGhost();
    }

    private void ToggleRecording()
    {
        if (!recorder.IsRecording)
        {
            recorder.StartRecording();
            return;
        }

        recorder.StopRecording();
        _lastRecording = ScriptableObject.CreateInstance<StateReplay>();
        recorder.PopulateReplay(_lastRecording);
        Debug.Log($"[OverlayTester] Recording captured ({_lastRecording.duration:F1}s). Press {playKey} to replay the ghost.");
    }

    private void PlayGhost()
    {
        // A saved asset takes precedence: it's the fresh-run workflow (record,
        // save, restart play mode, replay on a pristine level). The in-memory
        // take is the quick same-run check.
        StateReplay replay = replayAsset != null ? replayAsset : _lastRecording;
        if (replay == null)
        {
            Debug.LogWarning($"[OverlayTester] Nothing to play — assign a Replay Asset or press {recordKey} to record a run.");
            return;
        }
        if (_ghost != null) Destroy(_ghost);

        GameObject source = ghostPrefab != null ? ghostPrefab
            : recorder.target != null ? recorder.target.gameObject
            : ResolveLivePlayerObject();
        if (source == null)
        {
            Debug.LogWarning("[OverlayTester] No ghost source (no ghostPrefab, no recorder target, no local player found).");
            return;
        }

        _ghost = Instantiate(source);
        _ghost.name = "ReplayGhost";
        // Same-frame neutralize: gameplay components are disabled before their
        // Start ever runs, so the clone never acts on the world.
        StateReplayDriver.AttachGhost(_ghost, replay, recorder.levelRoot);
    }

    /// <summary>Fresh-run fallback ghost source: the live local player (nothing was recorded this session).</summary>
    private static GameObject ResolveLivePlayerObject()
    {
        Player player = GameManager.Instance != null ? GameManager.Instance.GetLocalPlayer() : null;
        if (player == null) player = FindObjectOfType<Player>();
        return player != null ? player.gameObject : null;
    }

    private void OnGUI()
    {
        string status = recorder.IsRecording
            ? $"● REC {recorder.RecordedSeconds:F1}s ({recorder.PlayerSampleCount} samples)"
            : replayAsset != null
                ? $"Asset '{replayAsset.name}' ({replayAsset.duration:F1}s) — {playKey} to replay ghost"
                : _lastRecording != null
                    ? $"Recorded {_lastRecording.duration:F1}s — {playKey} to replay ghost"
                    : $"{recordKey} to record";
        GUI.Label(new Rect(10, 10, 500, 22), $"[Replay Overlay] {status}");
    }
}
