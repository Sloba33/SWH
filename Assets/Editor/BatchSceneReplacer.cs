using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class BatchSceneReplacer : EditorWindow
{
    [Header("Scene List")]
    public List<SceneAsset> scenesToFix = new();

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
        EditorGUILayout.LabelField("Scenes to Fix", EditorStyles.boldLabel);
        SerializedObject so = new SerializedObject(this);
        SerializedProperty scenesProp = so.FindProperty("scenesToFix");
        EditorGUILayout.PropertyField(scenesProp, true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        environmentPrefab = (GameObject)EditorGUILayout.ObjectField("Environment Prefab", environmentPrefab, typeof(GameObject), false);
        spSettingsPrefab = (GameObject)EditorGUILayout.ObjectField("SP_Controls Prefab", spSettingsPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix All Scenes"))
        {
            FixScenes();
        }
    }

    private void FixScenes()
    {
        if (scenesToFix == null || scenesToFix.Count == 0)
        {
            Debug.LogWarning("No scenes assigned to fix.");
            return;
        }

        foreach (SceneAsset sceneAsset in scenesToFix)
        {
            if (sceneAsset == null)
                continue;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            bool modified = false;

            // Replace Environment
            GameObject oldEnvironment = GameObject.Find("Environment");
            if (oldEnvironment != null && environmentPrefab != null)
            {
                Vector3 pos = oldEnvironment.transform.position;
                Quaternion rot = oldEnvironment.transform.rotation;
                GameObject newEnv = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab);
                newEnv.transform.position = pos;
                newEnv.transform.rotation = rot;

                Undo.DestroyObjectImmediate(oldEnvironment);
                modified = true;
                Debug.Log($"Replaced Environment in {scene.name}");
            }

            // Replace SP_Controls under Main_UI
            GameObject spSettings = GameObject.Find("SP_Settings");
            if (spSettings != null && spSettingsPrefab != null)
            {
                GameObject mainUI = GameObject.Find("Main_UI");
                if (mainUI != null)
                {
                    Vector3 pos = spSettings.transform.position;
                    Quaternion rot = spSettings.transform.rotation;
                    GameObject newSP = (GameObject)PrefabUtility.InstantiatePrefab(spSettingsPrefab, mainUI.transform);
                    newSP.transform.position = pos;
                    newSP.transform.rotation = rot;

                    Undo.DestroyObjectImmediate(spSettings);
                    modified = true;
                    Debug.Log($"Replaced SP_Controls in {scene.name}");
                }
            }

            // Assign all obstacles to LevelGoal
            LevelGoal levelGoal = GameObject.FindObjectOfType<LevelGoal>();
            if (levelGoal != null)
            {
                Obstacle[] allObstacles = GameObject.FindObjectsOfType<Obstacle>(true);
                levelGoal.ObstaclesToDestroy_Player.Clear();
                levelGoal.ObstaclesToDestroy_Player.AddRange(allObstacles);
                EditorUtility.SetDirty(levelGoal);
                modified = true;
                Debug.Log($"Added {allObstacles.Length} obstacles to LevelGoal in {scene.name}");
            }
            else
            {
                Debug.LogWarning($"No LevelGoal found in {scene.name}");
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Saved scene: {scene.name}");
            }
            else
            {
                Debug.Log($"No changes needed for scene: {scene.name}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Batch scene fix completed.");
    }
}
