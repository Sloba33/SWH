using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class BatchLevelGoalEditor : EditorWindow
{
    // Fields shown in Inspector
    public List<SceneAsset> scenes = new List<SceneAsset>();
    public int xpReward = 0;
    public int trophyReward = 0;

    private SerializedObject so;
    private SerializedProperty spScenes;

    [MenuItem("Tools/Batch/LevelGoal Editor")]
    public static void OpenWindow()
    {
        GetWindow<BatchLevelGoalEditor>("Batch LevelGoal Editor");
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
        spScenes = so.FindProperty("scenes");
    }

    private void OnGUI()
    {
        so.Update();

        EditorGUILayout.LabelField("Batch Reward Assignment Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(spScenes, true);

        xpReward = EditorGUILayout.IntField("XP Reward", xpReward);
        trophyReward = EditorGUILayout.IntField("Trophy Reward", trophyReward);

        EditorGUILayout.Space();

        if (GUILayout.Button("Run Tool (Apply to All Scenes)", GUILayout.Height(40)))
        {
            RunTool();
        }

        so.ApplyModifiedProperties();
    }

    private void RunTool()
    {
        if (scenes.Count == 0)
        {
            Debug.LogError("No scenes assigned!");
            return;
        }

        foreach (var sceneAsset in scenes)
        {
            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);

            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning($"Skipping invalid scene entry.");
                continue;
            }

            // Open Scene
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find LevelGoal
            LevelGoal goal = FindObjectOfType<LevelGoal>();

            if (goal == null)
            {
                Debug.LogWarning($"No LevelGoal found in scene: {sceneAsset.name}");
                continue;
            }

            // Apply changes
            Undo.RecordObject(goal, "Update LevelGoal Values");
            goal.xp = xpReward;
            goal.trophies = trophyReward;

            EditorUtility.SetDirty(goal);

            // Save scene
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log($"Updated LevelGoal in '{sceneAsset.name}' to XP={xpReward}, Trophies={trophyReward}");
        }

        Debug.Log("Batch LevelGoal edit completed!");
    }
}
