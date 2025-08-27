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
    public bool OffsetEnvironment;

    [Header("Environment Settings")]
    public GameObject environmentPrefab;
    public bool UseCustomEnvironmentName;
    public string EnvironmentObjectName = "Environment";
    public Vector3 EnvironmentOffset = Vector3.zero;
    public string ExtraObjectName1 = "";
    public string ExtraObjectName2 = "";

    [Header("SP_Settings Settings")]
    public GameObject spSettingsPrefab;
    public bool UseCustomSettingsName;
    public string SettingsObjectName = "SP_Settings";

    [Header("Dialogue Settings")]
    public bool UseCustomDialogueName;
    public string DialogueObjectName = "Dialogue";

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

        // --- Environment ---
        ShouldFixEnvironment = EditorGUILayout.Toggle("Fix Environment", ShouldFixEnvironment);
        using (new EditorGUI.DisabledScope(!ShouldFixEnvironment))
        {
            environmentPrefab = (GameObject)EditorGUILayout.ObjectField("Environment Prefab", environmentPrefab, typeof(GameObject), false);

            UseCustomEnvironmentName = EditorGUILayout.Toggle("Use Custom Env Name", UseCustomEnvironmentName);
            using (new EditorGUI.DisabledScope(!UseCustomEnvironmentName))
            {
                EnvironmentObjectName = EditorGUILayout.TextField("Env Object Name", EnvironmentObjectName);
            }

            OffsetEnvironment = EditorGUILayout.Toggle("Offset Environment", OffsetEnvironment);
            using (new EditorGUI.DisabledScope(!OffsetEnvironment))
            {
                EnvironmentOffset = EditorGUILayout.Vector3Field("Environment Offset", EnvironmentOffset);
            }

            // Optional extra removals alongside Environment swap
            ExtraObjectName1 = EditorGUILayout.TextField("Extra Object Name 1", ExtraObjectName1);
            ExtraObjectName2 = EditorGUILayout.TextField("Extra Object Name 2", ExtraObjectName2);
        }

        // --- SP_Settings ---
        ShouldFixSettings = EditorGUILayout.Toggle("Fix Settings (SP_Settings)", ShouldFixSettings);
        using (new EditorGUI.DisabledScope(!ShouldFixSettings))
        {
            spSettingsPrefab = (GameObject)EditorGUILayout.ObjectField("SP_Settings Prefab", spSettingsPrefab, typeof(GameObject), false);

            UseCustomSettingsName = EditorGUILayout.Toggle("Use Custom Settings Name", UseCustomSettingsName);
            using (new EditorGUI.DisabledScope(!UseCustomSettingsName))
            {
                SettingsObjectName = EditorGUILayout.TextField("Settings Obj Name", SettingsObjectName);
            }
        }

        // --- Dialogue ---
        ShouldRemoveDialogueIcon = EditorGUILayout.Toggle("Disable Dialogue UI", ShouldRemoveDialogueIcon);
        using (new EditorGUI.DisabledScope(!ShouldRemoveDialogueIcon))
        {
            UseCustomDialogueName = EditorGUILayout.Toggle("Use Custom Dialogue Name", UseCustomDialogueName);
            using (new EditorGUI.DisabledScope(!UseCustomDialogueName))
            {
                DialogueObjectName = EditorGUILayout.TextField("Dialogue Obj Name", DialogueObjectName);
            }
        }

        // --- LevelGoal + Obstacles ---
        ShouldFixLevelGoal = EditorGUILayout.Toggle("Fix LevelGoal Obstacles", ShouldFixLevelGoal);
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
                string envName = UseCustomEnvironmentName ? EnvironmentObjectName : "Environment";
                GameObject oldEnv = GameObject.Find(envName);

                if (oldEnv && environmentPrefab)
                {
                    // Destroy old first so name conflicts/parents don’t interfere
                    Undo.DestroyObjectImmediate(oldEnv);

                    // Instantiate prefab into this scene and keep its own transform (usually 0,0,0)
                    GameObject newEnv = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab, scene);

                    // If offset is requested, add it on top of the prefab’s own position
                    if (OffsetEnvironment)
                        newEnv.transform.position += EnvironmentOffset;

                    modified = true;
                    Debug.Log($"Replaced Environment in {scene.name} (kept prefab's transform)");
                }

                // Optional extra removals
                if (!string.IsNullOrEmpty(ExtraObjectName1))
                {
                    GameObject extra1 = GameObject.Find(ExtraObjectName1);
                    if (extra1)
                    {
                        Undo.DestroyObjectImmediate(extra1);
                        modified = true;
                        Debug.Log($"Removed extra object {ExtraObjectName1} in {scene.name}");
                    }
                }
                if (!string.IsNullOrEmpty(ExtraObjectName2))
                {
                    GameObject extra2 = GameObject.Find(ExtraObjectName2);
                    if (extra2)
                    {
                        Undo.DestroyObjectImmediate(extra2);
                        modified = true;
                        Debug.Log($"Removed extra object {ExtraObjectName2} in {scene.name}");
                    }
                }
            }

            // --- Fix SP_Settings ---
            if (ShouldFixSettings)
            {
                GameObject mainUI = GameObject.Find("Main_UI");
                if (mainUI)
                {
                    string settingsName = UseCustomSettingsName ? SettingsObjectName : "SP_Settings";
                    GameObject oldSP = GameObject.Find(settingsName);
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
                string dialogueName = UseCustomDialogueName ? DialogueObjectName : "dialogue";
                GameObject[] allObjs = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var go in allObjs)
                {
                    if (go.name.ToLower().Contains(dialogueName.ToLower()))
                    {
                        go.SetActive(false);
                        modified = true;
                        Debug.Log($"Disabled Dialogue Object: {go.name} in {scene.name}");
                    }
                }
            }

            // --- Fix LevelGoal + Obstacles ---
            LevelGoal levelGoal = GameObject.FindFirstObjectByType<LevelGoal>();
            if (levelGoal)
            {
                Obstacle[] obstacles = GameObject.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);

                if (ShouldFixLevelGoal)
                {
                    List<Obstacle> validObstacles = new();
                    foreach (var obs in obstacles)
                    {
                        if (obs.obstacleType != ObstacleType.Metal) // exclude metal boxes
                            validObstacles.Add(obs);
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
                        obs.transform.SetParent(obstacleParent.transform, true); // keep world pos
                    }

                    Debug.Log($"Grouped {obstacles.Length} obstacles under 'Obstacles' in {scene.name}");
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
