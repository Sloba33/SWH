using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
    public bool ShouldReplaceSceneName;
    public bool ShouldReorderSceneName;

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

    [Header("Name Replace Settings")]
    public string TargetString = "ToyFactory";
    public string ResultString = "City";

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

        // --- Name Editing ---
        ShouldReplaceSceneName = EditorGUILayout.Toggle("Replace Target String in Scene Name", ShouldReplaceSceneName);
        using (new EditorGUI.DisabledScope(!ShouldReplaceSceneName))
        {
            TargetString = EditorGUILayout.TextField("Target String", TargetString);
            ResultString = EditorGUILayout.TextField("Result String", ResultString);
        }

        ShouldReorderSceneName = EditorGUILayout.Toggle("Reorder Scene Name (Name - Grid - Number)", ShouldReorderSceneName);

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

            // --- Scene Name Replace ---
            string path = scene.path;
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (ShouldReplaceSceneName && !string.IsNullOrEmpty(TargetString))
            {
                if (sceneName.Contains(TargetString))
                {
                    string newSceneName = sceneName.Replace(TargetString, ResultString);
                    AssetDatabase.RenameAsset(path, newSceneName);
                    sceneName = newSceneName;
                    modified = true;
                    Debug.Log($"Renamed {scene.name} -> {sceneName}");
                }
            }
            if (ShouldReorderSceneName)
            {
                // Regex: captures Name, Grid (e.g. 10x10), and Number
                // Handles both "Name - 10x10 200" and "Name - 10x10 - 200"
                Match match = Regex.Match(sceneName, @"^(.*?)[\s-]+(\d+x\d+)[\s-]+(\d+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    Debug.Log("Old Scene name : " + sceneName);
                    string name = match.Groups[1].Value.Trim();
                    string grid = match.Groups[2].Value.Trim();
                    string number = match.Groups[3].Value.Trim();

                    string newSceneName = $"{name} - {number} - {grid}";
                    if (!sceneName.Equals(newSceneName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        AssetDatabase.RenameAsset(path, newSceneName);
                        sceneName = newSceneName;
                        modified = true;
                        Debug.Log($"Reordered scene name -> {sceneName}");
                    }
                }
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
