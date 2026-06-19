using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for <see cref="BotRecorder"/>: start/stop recording during play and
/// save the captured session as a <see cref="BotReplay"/> asset.
/// </summary>
[CustomEditor(typeof(BotRecorder))]
public class BotRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var recorder = (BotRecorder)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter play mode to record. Assign Level Root (the level parent you'll play " +
                                    "under); positions are stored local to it.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Status", recorder.IsRecording
            ? $"● Recording — {recorder.ActionCount} actions"
            : (recorder.ActionCount > 0 ? $"Stopped — {recorder.ActionCount} actions captured" : "Idle"));

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (!recorder.IsRecording)
            {
                if (GUILayout.Button("● Start Recording", GUILayout.Height(30)))
                    recorder.StartRecording();
            }
            else
            {
                if (GUILayout.Button("■ Stop Recording", GUILayout.Height(30)))
                    recorder.StopRecording();
            }
        }

        using (new EditorGUI.DisabledScope(recorder.IsRecording || recorder.ActionCount == 0))
        {
            if (GUILayout.Button("Save as Replay Asset…", GUILayout.Height(26)))
                SaveReplayAsset(recorder);
        }

        // Live-refresh the status while recording.
        if (recorder.IsRecording) Repaint();
    }

    private static void SaveReplayAsset(BotRecorder recorder)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Bot Replay", "BotReplay", "asset",
            "Choose where to save the recorded replay. (Put it under a Resources/BotReplays folder to use it as a matchmaking fallback.)");
        if (string.IsNullOrEmpty(path)) return;

        BotReplay replay = ScriptableObject.CreateInstance<BotReplay>();
        recorder.PopulateReplay(replay);
        AssetDatabase.CreateAsset(replay, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(replay);
        Selection.activeObject = replay;

        bool sceneSet = replay.scene != null && replay.scene.UnsafeReason == Eflatun.SceneReference.SceneReferenceUnsafeReason.None;
        Debug.Log($"[BotRecorder] Saved replay with {replay.actions.Count} actions to {path}. " +
                  (sceneSet
                      ? $"Scene set to '{replay.scene.Name}'."
                      : "Scene reference could not be auto-set — assign it manually."));
    }
}
