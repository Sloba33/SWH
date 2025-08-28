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
    public bool ShouldFixControls;
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

    [Header("SP_Controls Settings")]
    public GameObject spControlsPrefab;
    public bool UseCustomControlsName;
    public string ControlsObjectName = "SP_Controls";

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

        // --- SP_Controls ---
        ShouldFixControls = EditorGUILayout.Toggle("Fix Controls (SP_Controls)", ShouldFixControls);
        using (new EditorGUI.DisabledScope(!ShouldFixControls))
        {
            spControlsPrefab = (GameObject)EditorGUILayout.ObjectField("SP_Controls Prefab", spControlsPrefab, typeof(GameObject), false);

            UseCustomControlsName = EditorGUILayout.Toggle("Use Custom Controls Name", UseCustomControlsName);
            using (new EditorGUI.DisabledScope(!UseCustomControlsName))
            {
                ControlsObjectName = EditorGUILayout.TextField("Controls Obj Name", ControlsObjectName);
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
                    Undo.DestroyObjectImmediate(oldEnv);
                    GameObject newEnv = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab, scene);
                    if (OffsetEnvironment) newEnv.transform.position += EnvironmentOffset;
                    modified = true;
                    Debug.Log($"Replaced Environment in {scene.name}");
                }

                if (!string.IsNullOrEmpty(ExtraObjectName1))
                {
                    GameObject extra1 = GameObject.Find(ExtraObjectName1);
                    if (extra1) { Undo.DestroyObjectImmediate(extra1); modified = true; }
                }
                if (!string.IsNullOrEmpty(ExtraObjectName2))
                {
                    GameObject extra2 = GameObject.Find(ExtraObjectName2);
                    if (extra2) { Undo.DestroyObjectImmediate(extra2); modified = true; }
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

            // --- Fix SP_Controls ---
            if (ShouldFixControls)
            {
                GameObject mainUI = GameObject.Find("Main_UI");
                if (mainUI)
                {
                    string controlsName = UseCustomControlsName ? ControlsObjectName : "SP_Controls";
                    GameObject oldControls = GameObject.Find(controlsName);
                    if (oldControls && spControlsPrefab)
                    {
                        Vector3 pos = oldControls.transform.position;
                        Quaternion rot = oldControls.transform.rotation;

                        GameObject newControls = (GameObject)PrefabUtility.InstantiatePrefab(spControlsPrefab, mainUI.transform);
                        newControls.transform.position = pos;
                        newControls.transform.rotation = rot;

                        Undo.DestroyObjectImmediate(oldControls);
                        modified = true;
                        Debug.Log($"Replaced SP_Controls in {scene.name}");
                    }
                }
            }

            // --- Disable Dialogue ---
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
                        if (obs.obstacleType != ObstacleType.Metal)
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
                        obs.transform.SetParent(obstacleParent.transform, true);

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
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Finished processing all scenes.");
    }
}
