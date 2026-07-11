using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-stop replay recording tool for designers/artists
/// (Tools/SWH/Replay/Record Replay In This Scene…).
///
/// Open the level scene you want a bot replay for, optionally set the stats to
/// record with, and press Start: the editor enters play mode as a take session
/// (level frozen, recording starts on your first action, save dialog appears on
/// win/death — see ReplayTakeController). Stat overrides feed
/// Player.ApplyLocalPlayerStats via ReplayTakeSession, replacing the upgrade
/// system's values for this take only.
/// </summary>
public class ReplayTakeWindow : EditorWindow
{
    // EditorPrefs (not SessionState) so the chosen values survive editor restarts.
    private const string PrefOverride = "SWH.ReplayTakeWindow.OverrideStats";
    private const string PrefMoveSpeed = "SWH.ReplayTakeWindow.MoveSpeed";
    private const string PrefStrength = "SWH.ReplayTakeWindow.Strength";

    private bool _overrideStats;
    private float _moveSpeed;
    private float _strength;

    [MenuItem("Tools/SWH/Replay/Record Replay In This Scene…", priority = 0)]
    private static void Open()
    {
        var window = GetWindow<ReplayTakeWindow>("Record Replay");
        window.minSize = new Vector2(320f, 190f);
    }

    private void OnEnable()
    {
        _overrideStats = EditorPrefs.GetBool(PrefOverride, false);
        _moveSpeed = EditorPrefs.GetFloat(PrefMoveSpeed, 2f);
        _strength = EditorPrefs.GetFloat(PrefStrength, 10f);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene", SceneManager.GetActiveScene().name);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Recording Stats", EditorStyles.boldLabel);
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            _overrideStats = EditorGUILayout.ToggleLeft(
                new GUIContent("Override stats for this take",
                    "Off: record with the upgrade system's stats (same as a real match). " +
                    "On: record with the exact values below."),
                _overrideStats);

            using (new EditorGUI.DisabledScope(!_overrideStats))
            {
                _moveSpeed = EditorGUILayout.FloatField("Move Speed", _moveSpeed);
                _strength = EditorGUILayout.FloatField("Strength", _strength);
            }

            if (check.changed)
            {
                EditorPrefs.SetBool(PrefOverride, _overrideStats);
                EditorPrefs.SetFloat(PrefMoveSpeed, _moveSpeed);
                EditorPrefs.SetFloat(PrefStrength, _strength);
            }
        }

        EditorGUILayout.Space();

        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                ReplayTakeSession.IsActive
                    ? "Take in progress — the level unfreezes and recording starts on your first action. " +
                      "Win or die to get the save dialog."
                    : "Play mode is running, but not as a take. Exit play mode to start one.",
                MessageType.Info);
            return;
        }

        if (GUILayout.Button("●  Start Recording Take", GUILayout.Height(36f)))
        {
            ReplayTakeSession.IsActive = true;
            ReplayTakeSession.OverrideStats = _overrideStats;
            ReplayTakeSession.MoveSpeed = _moveSpeed;
            ReplayTakeSession.Strength = _strength;
            EditorApplication.EnterPlaymode();
        }

        EditorGUILayout.HelpBox(
            "Starts play mode in the current scene with the level frozen. Recording begins on your first " +
            "action; winning or dying opens the save dialog (Assets/_GAME/04_Data).",
            MessageType.None);
    }

    // Clear the session flag whenever play mode ends so a later normal play
    // session can't accidentally start as a take.
    [InitializeOnLoadMethod]
    private static void InstallCleanupHook()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                ReplayTakeSession.IsActive = false;
        };
    }
}
