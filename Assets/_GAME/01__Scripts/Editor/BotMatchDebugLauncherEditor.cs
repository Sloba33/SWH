using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for <see cref="BotMatchDebugLauncher"/>: a quick-pick dropdown of
/// every BotReplay in the project, a preview of the scene that will load, and a
/// one-click "enter play mode & launch" button.
/// </summary>
[CustomEditor(typeof(BotMatchDebugLauncher))]
public class BotMatchDebugLauncherEditor : Editor
{
    private BotReplay[] _replays = new BotReplay[0];
    private string[] _replayNames = new string[0];

    private void OnEnable() => RefreshReplayList();

    private void RefreshReplayList()
    {
        string[] guids = AssetDatabase.FindAssets("t:BotReplay");
        _replays = new BotReplay[guids.Length];
        _replayNames = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            _replays[i] = AssetDatabase.LoadAssetAtPath<BotReplay>(path);
            _replayNames[i] = _replays[i] != null ? _replays[i].name : path;
        }
    }

    public override void OnInspectorGUI()
    {
        var launcher = (BotMatchDebugLauncher)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Pick", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            int current = System.Array.IndexOf(_replays, launcher.replay);
            int picked = EditorGUILayout.Popup("All Replays", current, _replayNames);
            if (picked != current && picked >= 0 && picked < _replays.Length)
            {
                Undo.RecordObject(launcher, "Pick Bot Replay");
                launcher.replay = _replays[picked];
                EditorUtility.SetDirty(launcher);
            }
            if (GUILayout.Button("↻", GUILayout.Width(26)))
                RefreshReplayList();
        }

        EditorGUILayout.Space();
        string scene = launcher.ResolveSceneName();
        EditorGUILayout.LabelField("Resolved Scene", string.IsNullOrEmpty(scene) ? "(none)" : scene);

        EditorGUILayout.Space();
        if (!Application.isPlaying)
        {
            using (new EditorGUI.DisabledScope(launcher.replay == null || string.IsNullOrEmpty(scene)))
            {
                if (GUILayout.Button("▶ Enter Play Mode & Launch", GUILayout.Height(32)))
                {
                    launcher.launchOnStart = true;
                    EditorUtility.SetDirty(launcher);
                    EditorApplication.isPlaying = true;
                }
            }
            EditorGUILayout.HelpBox("Press play with this debug scene open, or use the button above. " +
                                    "The target scene must be in Build Settings.", MessageType.Info);
        }
        else
        {
            if (GUILayout.Button("Re-Launch Now", GUILayout.Height(32)))
                launcher.Launch();
        }
    }
}
