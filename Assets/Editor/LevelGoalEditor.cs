#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelGoal))]
public class LevelGoalEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        LevelGoal goal = (LevelGoal)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBoxSpawn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBombSpawn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayCollectibleSpawn"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("ObstacleSpawnFrequency"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bombSpawnFrequency"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("collectibleSpawnFreqency"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("minObstacleSpawnHeight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxObstacleSpawnHeight"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useNewSpawnSystem"));

        if (goal.useNewSpawnSystem)
        {
            EditorGUILayout.LabelField("New Fixed Spawn System", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedFallingObstacles"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedFallingBombs"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedFallingCollectibles"), true);

            GUI.enabled = false;
            EditorGUILayout.LabelField("Old Weighted Spawn System (Disabled)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FallingObstacles"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FallingBombs"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FallingCollectibles"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstaclesToSpawn"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bombsToSpawn"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiblesToSpawn"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnFallingObstacles"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnFallingBombs"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnFallingCollectibles"));
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.LabelField("Old Weighted Spawn System", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FallingObstacles"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FallingBombs"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FallingCollectibles"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstaclesToSpawn"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bombsToSpawn"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiblesToSpawn"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnFallingObstacles"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnFallingBombs"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnFallingCollectibles"));

            GUI.enabled = false;
            EditorGUILayout.LabelField("New Fixed Spawn System (Disabled)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedFallingObstacles"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedFallingBombs"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedFallingCollectibles"), true);
            GUI.enabled = true;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
