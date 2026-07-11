using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for <see cref="StateReplayRecorder"/>: start/stop recording during
/// play and save the captured session as a <see cref="StateReplay"/> asset.
/// </summary>
[CustomEditor(typeof(StateReplayRecorder))]
public class StateReplayRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var recorder = (StateReplayRecorder)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter play mode to record. Assign Level Root (the ReplayScope transform of " +
                                    "the half you'll play); positions are stored local to it.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Status", recorder.IsRecording
            ? $"● Recording — {recorder.RecordedSeconds:F1}s, {recorder.PlayerSampleCount} samples, {recorder.AnimEventCount} anim events"
            : (recorder.PlayerSampleCount > 0
                ? $"Stopped — {recorder.RecordedSeconds:F1}s captured"
                : "Idle"));

        EditorGUILayout.Space();
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

        using (new EditorGUI.DisabledScope(recorder.IsRecording || recorder.PlayerSampleCount == 0))
        {
            if (GUILayout.Button("Save as State Replay Asset…", GUILayout.Height(26)))
                SaveReplayAsset(recorder);
        }

        if (recorder.IsRecording) Repaint();
    }

    private static void SaveReplayAsset(StateReplayRecorder recorder)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save State Replay", "StateReplay", "asset",
            "Choose where to save the recorded replay.");
        if (string.IsNullOrEmpty(path)) return;

        StateReplay replay = ScriptableObject.CreateInstance<StateReplay>();
        recorder.PopulateReplay(replay);
        // Same parseable label format as ReplayTakeController (minus the outcome,
        // which only the take flow knows).
        if (recorder.target != null)
        {
            replay.label = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "speed={0:0.##} strength={1:0.##} duration={2}",
                recorder.target.StartingMoveSpeed, recorder.target.StartingStrenght,
                Mathf.RoundToInt(replay.duration));
        }
        AssetDatabase.CreateAsset(replay, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(replay);
        Selection.activeObject = replay;

        bool sceneSet = replay.scene != null && replay.scene.UnsafeReason == Eflatun.SceneReference.SceneReferenceUnsafeReason.None;
        Debug.Log($"[StateReplayRecorder] Saved {replay.duration:F1}s replay " +
                  $"({replay.playerTrack.Count} samples, {replay.playerAnimTrack.Count} anim events) to {path}. " +
                  (sceneSet ? $"Scene set to '{replay.scene.Name}'." : "Scene reference could not be auto-set — assign it manually."));
    }
}
