#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

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
    public bool ShouldFixObstacleWeights;
    public bool ShouldFixDefaultZoom;
    public bool AddBackToMainMenuButton; // <-- New Toggle Added

    // Added new field for Default Zoom value
    [Header("Camera Zoom Settings")]
    public int DefaultZoomValue = 2;


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
    
    [Header("Obstacle Weight Modifier")]
    public float ObstacleWeightModifier = 1f;
    public bool UseIncrementalWeight;
    public float WeightIncrement = 0.03f;
    public int ScenesPerIncrement = 3;

    [MenuItem("Tools/Batch Scene Fixer")]
    public static void ShowWindow()
    {
        GetWindow<BatchSceneReplacer>("Batch Scene Fixer");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);

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
        
        // --- Obstacle Weight Modifier ---
        ShouldFixObstacleWeights = EditorGUILayout.Toggle("Modify Obstacle Weights", ShouldFixObstacleWeights);
        using (new EditorGUI.DisabledScope(!ShouldFixObstacleWeights))
        {
            ObstacleWeightModifier = EditorGUILayout.FloatField("Base Weight Modifier", ObstacleWeightModifier);
            
            UseIncrementalWeight = EditorGUILayout.Toggle("Use Incremental Weight", UseIncrementalWeight);
            using (new EditorGUI.DisabledScope(!UseIncrementalWeight))
            {
                WeightIncrement = EditorGUILayout.FloatField("Weight Increment", WeightIncrement);
                ScenesPerIncrement = EditorGUILayout.IntField("Scenes Per Increment", ScenesPerIncrement);
            }
        }

        // --- Camera Zoom Modifier ---
        ShouldFixDefaultZoom = EditorGUILayout.Toggle("Modify Camera Zoom", ShouldFixDefaultZoom);
        using (new EditorGUI.DisabledScope(!ShouldFixDefaultZoom))
        {
            DefaultZoomValue = EditorGUILayout.IntField("Default Zoom", DefaultZoomValue);
        }

        // --- New: Back to Main Menu Button ---
        AddBackToMainMenuButton = EditorGUILayout.Toggle("Add Back to Main Menu Button", AddBackToMainMenuButton);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Fix All Scenes"))
        {
            FixScenes();
        }

        so.ApplyModifiedProperties();
    }

    private void FixScenes()
    {
        int sceneCounter = 0;
        float currentWeightModifier = ObstacleWeightModifier;

        foreach (SceneAsset sceneAsset in scenesToFix)
        {
            if (sceneAsset == null) continue;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            bool modified = false;

            // --- Existing Scene Fixes (omitted for brevity, assume they are still here) ---
            
            // ... (All previous fixes like Environment, Settings, Controls, Dialogue, Naming, LevelGoal, Obstacles)

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
                    GameObject oldSettings = GameObject.Find(settingsName);
                    if (oldSettings && spSettingsPrefab)
                    {
                        Vector3 pos = oldSettings.transform.position;
                        Quaternion rot = oldSettings.transform.rotation;

                        GameObject newSettings = (GameObject)PrefabUtility.InstantiatePrefab(spSettingsPrefab, mainUI.transform);
                        newSettings.transform.position = pos;
                        newSettings.transform.rotation = rot;

                        Undo.DestroyObjectImmediate(oldSettings);
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

            // --- Replace Scene Name ---
            string path = scene.path;
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (ShouldReplaceSceneName && !string.IsNullOrEmpty(TargetString) && sceneName.Contains(TargetString))
            {
                string newSceneName = sceneName.Replace(TargetString, ResultString);
                AssetDatabase.RenameAsset(path, newSceneName);
                modified = true;
                Debug.Log($"Renamed Scene: {sceneName} → {newSceneName}");
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
                        if (obs.obstacleType != ObstacleType.Metal && obs.obstacleType != ObstacleType.Concrete && obs.obstacleType != ObstacleType.Cardboard)
                            validObstacles.Add(obs);
                    }
                    levelGoal.ObstaclesToDestroy_Player.Clear();
                    levelGoal.ObstaclesToDestroy_Player.AddRange(validObstacles);
                    EditorUtility.SetDirty(levelGoal);
                    modified = true;
                    Debug.Log($"Fixed LevelGoal's ObstaclesToDestroy list in {scene.name}");
                }

                if (ShouldParentAllObstacles)
                {
                    GameObject obstacleParent = GameObject.Find("Obstacles") ?? new GameObject("Obstacles");
                    obstacleParent.transform.position = Vector3.zero;
                    foreach (Obstacle obs in obstacles)
                        obs.transform.SetParent(obstacleParent.transform, true);

                    Debug.Log($"Grouped {obstacles.Length} obstacles under 'Obstacles' in {scene.name}");
                    modified = true;
                }
            }
            else
            {
                Debug.LogWarning($"No LevelGoal found in scene {scene.name}");
            }

            // --- Fix Obstacle Weights ---
            GameManager gm = GameObject.FindFirstObjectByType<GameManager>();
            
            if (ShouldFixObstacleWeights && gm != null)
            {
                // Calculate the current weight modifier based on incremental settings
                float weightToApply = ObstacleWeightModifier;
                
                if (UseIncrementalWeight && ScenesPerIncrement > 0)
                {
                    int incrementGroup = sceneCounter / ScenesPerIncrement;
                    weightToApply = ObstacleWeightModifier + (incrementGroup * WeightIncrement);
                }
                
                gm.ObstacleWeightModifier = weightToApply;
                EditorUtility.SetDirty(gm);
                modified = true;
                Debug.Log($"Set obstacle weight modifier to {weightToApply} in scene {scene.name} (scene #{sceneCounter + 1})");
            }
            
            // --- FIX CAMERA ZOOM ---
            if (ShouldFixDefaultZoom && gm != null)
            {
                gm.defaultZoomValue = DefaultZoomValue;
                EditorUtility.SetDirty(gm);
                modified = true;
                Debug.Log($"Set GameManager.defaultZoomValue to {DefaultZoomValue} in scene {scene.name}");
            }

            // --- NEW: ADD BACK TO MAIN MENU BUTTON ---
            if (AddBackToMainMenuButton)
            {
                if (gm == null)
                {
                    gm = GameObject.FindFirstObjectByType<GameManager>(); // Try one more time in case it was missed above
                }
                
                if (gm != null)
                {
                    gm.ShouldHaveMainMenuButton = true;
                    EditorUtility.SetDirty(gm);
                    modified = true;
                    Debug.Log($"Set GameManager.ShouldHaveMainMenuButton to TRUE in scene {scene.name}");
                }
                else
                {
                    Debug.LogWarning($"Cannot set Main Menu Button: No GameManager found in scene {scene.name}");
                }
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            sceneCounter++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Finished processing all scenes.");
    }
}
#endif