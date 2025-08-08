using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BatchSceneReplacer : EditorWindow
{
    [Header("Scene List")]
    public List<SceneAsset> scenesToFix = new();

    [Header("Toggles")]
    public bool ShouldFixEnvironment;
    public bool ShouldFixSettings;
    public bool ShouldRemoveDialogueIcon;
    public bool ShouldParentAllObstacles;
    public bool ShouldFixLevelGoal;

    [Header("Prefabs")]
    public GameObject environmentPrefab;
    public GameObject spSettingsPrefab;

    [MenuItem("Tools/Batch Scene Fixer")]
    public static void ShowWindow()
    {
        GetWindow<BatchSceneReplacer>("Batch Scene Fixer");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);

        // Scene List
        EditorGUILayout.PropertyField(so.FindProperty("scenesToFix"), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Modifications", EditorStyles.boldLabel);

        // Environment toggle and field
        ShouldFixEnvironment = EditorGUILayout.Toggle("Fix Environment", ShouldFixEnvironment);
        using (new EditorGUI.DisabledScope(!ShouldFixEnvironment))
        {
            environmentPrefab = (GameObject)EditorGUILayout.ObjectField("Environment Prefab", environmentPrefab, typeof(GameObject), false);
        }

        // SP Controls toggle and field
        ShouldFixSettings = EditorGUILayout.Toggle("Fix Settings (SP_Settings)", ShouldFixSettings);
        using (new EditorGUI.DisabledScope(!ShouldFixSettings))
        {
            spSettingsPrefab = (GameObject)EditorGUILayout.ObjectField("SP_Settings Prefab", spSettingsPrefab, typeof(GameObject), false);
        }

        // Dialogue disable toggle
        ShouldRemoveDialogueIcon = EditorGUILayout.Toggle("Disable Dialogue UI", ShouldRemoveDialogueIcon);

        // Parent obstacles toggle
        ShouldFixLevelGoal = EditorGUILayout.Toggle("Fix LevelGoal Obstacles", ShouldFixLevelGoal); ;
        ShouldParentAllObstacles = EditorGUILayout.Toggle("Group Obstacles in Scene", ShouldParentAllObstacles);

        EditorGUILayout.Space();
        if (GUILayout.Button("Fix All Scenes"))
        {
            FixScenes();
        }

        so.ApplyModifiedProperties();
    }

    private void FixScenes()
    {
        foreach (SceneAsset sceneAsset in scenesToFix)
        {
            if (sceneAsset == null) continue;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            bool modified = false;

            // --- Fix Environment ---
            if (ShouldFixEnvironment)
            {
                GameObject oldEnv = GameObject.Find("Environment");
                if (oldEnv && environmentPrefab)
                {
                    GameObject newEnv = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab);
                    Undo.DestroyObjectImmediate(oldEnv);
                    modified = true;
                    Debug.Log($"Replaced Environment in {scene.name}");
                }
            }

            // --- Fix SP_Controls ---
            if (ShouldFixSettings)
            {
                GameObject mainUI = GameObject.Find("Main_UI");
                if (mainUI)
                {
                    GameObject oldSP = GameObject.Find("SP_Settings");
                    if (oldSP && spSettingsPrefab)
                    {
                        Vector3 pos = oldSP.transform.position;
                        Quaternion rot = oldSP.transform.rotation;
                        GameObject newSP = (GameObject)PrefabUtility.InstantiatePrefab(spSettingsPrefab, mainUI.transform);
                        newSP.transform.position = pos;
                        newSP.transform.rotation = rot;
                        Undo.DestroyObjectImmediate(oldSP);
                        modified = true;
                        Debug.Log($"Replaced SP_Settings in {scene.name}");
                    }
                }
            }

            // --- Disable Dialogue UI ---
            if (ShouldRemoveDialogueIcon)
            {
                GameObject[] allObjs = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var go in allObjs)
                {
                    if (go.name.ToLower().Contains("dialogue"))
                    {
                        go.SetActive(false);
                        modified = true;
                        Debug.Log($"Disabled Dialogue Object: {go.name} in {scene.name}");
                    }
                }
            }

            // --- Group All Obstacles ---
            LevelGoal levelGoal = GameObject.FindFirstObjectByType<LevelGoal>();
            if (levelGoal)
            {
                Obstacle[] obstacles = GameObject.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
                if (ShouldFixLevelGoal)
                {

                    Obstacle[] allObstacles = GameObject.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
                    List<Obstacle> validObstacles = new();

                    foreach (var obs in allObstacles)
                    {
                        if (obs.obstacleType != ObstacleType.Metal)
                        {
                            validObstacles.Add(obs);
                        }
                    }

                    levelGoal.ObstaclesToDestroy_Player.Clear();
                    levelGoal.ObstaclesToDestroy_Player.AddRange(validObstacles);
                    EditorUtility.SetDirty(levelGoal);
                    modified = true;
                }

                if (ShouldParentAllObstacles)
                {
                    GameObject obstacleParent = GameObject.Find("Obstacles") ?? new GameObject("Obstacles");
                    obstacleParent.transform.position = Vector3.zero;

                    foreach (Obstacle obs in obstacles)
                    {
                        obs.transform.SetParent(obstacleParent.transform, true); // retain world pos
                    }

                    Debug.Log($"Grouped {obstacles.Length} obstacles under 'Obstacles' GameObject in {scene.name}");
                }
                else
                {
                    Debug.Log($"Assigned {obstacles.Length} obstacles to LevelGoal in {scene.name}");
                }
            }
            else
            {
                Debug.LogWarning($"No LevelGoal found in scene {scene.name}");
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Saved changes to scene: {scene.name}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Finished processing all scenes.");
    }
}
