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
    private Vector2 starTimesScrollPosition;
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
    public bool AddBackToMainMenuButton;

    // --- NEW: Star Time Toggles & Lists ---
    public bool ShouldFixStarTimes;
    public List<float> threeStarTimes = new();
    public float defaultOneStarTime = 5000f;
    [TextArea(3, 6)]
    public string pastedTimesText = ""; // For pasting from Excel/CSV

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
                EnvironmentObjectName = EditorGUILayout.TextField("Env Object Name", EnvironmentObjectName);

            OffsetEnvironment = EditorGUILayout.Toggle("Offset Environment", OffsetEnvironment);
            using (new EditorGUI.DisabledScope(!OffsetEnvironment))
                EnvironmentOffset = EditorGUILayout.Vector3Field("Environment Offset", EnvironmentOffset);

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
                SettingsObjectName = EditorGUILayout.TextField("Settings Obj Name", SettingsObjectName);
        }

        // --- SP_Controls ---
        ShouldFixControls = EditorGUILayout.Toggle("Fix Controls (SP_Controls)", ShouldFixControls);
        using (new EditorGUI.DisabledScope(!ShouldFixControls))
        {
            spControlsPrefab = (GameObject)EditorGUILayout.ObjectField("SP_Controls Prefab", spControlsPrefab, typeof(GameObject), false);
            UseCustomControlsName = EditorGUILayout.Toggle("Use Custom Controls Name", UseCustomControlsName);
            using (new EditorGUI.DisabledScope(!UseCustomControlsName))
                ControlsObjectName = EditorGUILayout.TextField("Controls Obj Name", ControlsObjectName);
        }

        // --- Dialogue ---
        ShouldRemoveDialogueIcon = EditorGUILayout.Toggle("Disable Dialogue UI", ShouldRemoveDialogueIcon);
        using (new EditorGUI.DisabledScope(!ShouldRemoveDialogueIcon))
        {
            UseCustomDialogueName = EditorGUILayout.Toggle("Use Custom Dialogue Name", UseCustomDialogueName);
            using (new EditorGUI.DisabledScope(!UseCustomDialogueName))
                DialogueObjectName = EditorGUILayout.TextField("Dialogue Obj Name", DialogueObjectName);
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
            DefaultZoomValue = EditorGUILayout.IntField("Default Zoom", DefaultZoomValue);

        // --- Back to Main Menu Button ---
        AddBackToMainMenuButton = EditorGUILayout.Toggle("Add Back to Main Menu Button", AddBackToMainMenuButton);

        // ==========================================
        // --- NEW: STAR TIME AUTOMATION UI ---
        // ==========================================
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Star Time Automation", EditorStyles.boldLabel);
        ShouldFixStarTimes = EditorGUILayout.Toggle("Auto-Set Star Times", ShouldFixStarTimes);

        using (new EditorGUI.DisabledScope(!ShouldFixStarTimes))
        {
            defaultOneStarTime = EditorGUILayout.FloatField("Default 1-Star Time (Fallback)", defaultOneStarTime);

            // Helper buttons for list management
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Times"))
            {
                threeStarTimes.Clear();
            }
            if (GUILayout.Button("Parse Pasted Text"))
            {
                ParseTimesFromText();
            }
            EditorGUILayout.EndHorizontal();

            // Text area for pasting from Excel/CSV
            EditorGUILayout.LabelField("Paste times from Excel (comma or newline separated):");
            pastedTimesText = EditorGUILayout.TextArea(pastedTimesText, GUILayout.Height(80));

            // --- Show warning if times count doesn't match scenes count ---
            SerializedProperty scenesProp = so.FindProperty("scenesToFix");
            SerializedProperty timesProp = so.FindProperty("threeStarTimes");

            int sceneCount = scenesProp.arraySize;
            int timeCount = timesProp.arraySize;

            if (sceneCount > 0 && timeCount < sceneCount)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ Mismatch: {timeCount} times defined but {sceneCount} scenes listed.\n" +
                    $"Scenes {timeCount} through {sceneCount - 1} will keep their existing LevelGoal times.",
                    MessageType.Warning
                );
            }
            else if (sceneCount > 0 && timeCount > sceneCount)
            {
                EditorGUILayout.HelpBox(
                    $"ℹ️ You have {timeCount} times but only {sceneCount} scenes.\n" +
                    $"Extra times beyond index {sceneCount - 1} will be ignored.",
                    MessageType.Info
                );
            }

            EditorGUILayout.LabelField("3-Star Times (Scroll to edit):");

            // Create a scroll view with a fixed height of 250 pixels
            starTimesScrollPosition = EditorGUILayout.BeginScrollView(starTimesScrollPosition, GUILayout.Height(250), GUILayout.ExpandWidth(true));

            // Show ALL times that exist, regardless of scene count
            for (int i = 0; i < timesProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Show the scene name if it exists, otherwise show "Extra time"
                string sceneName;
                Color labelColor = Color.white;

                if (i < scenesProp.arraySize)
                {
                    SerializedProperty sceneElement = scenesProp.GetArrayElementAtIndex(i);
                    sceneName = sceneElement.objectReferenceValue != null ? sceneElement.objectReferenceValue.name : "Missing Scene";
                }
                else
                {
                    sceneName = "⚠️ EXTRA TIME";
                    labelColor = Color.yellow;
                }

                // Color the label if it's an extra time
                GUI.color = labelColor;
                EditorGUILayout.LabelField($"{i,3}: {sceneName}", GUILayout.Width(220));
                GUI.color = Color.white;

                // Get the time value and format it as mm:ss
                SerializedProperty timeElement = timesProp.GetArrayElementAtIndex(i);
                float currentVal = timeElement.floatValue;

                int mins = Mathf.FloorToInt(currentVal / 60f);
                int secs = Mathf.FloorToInt(currentVal % 60f);
                string timeFormatted = $"{mins:00}:{secs:00}";

                // Draw the mm:ss label and the editable float field
                EditorGUILayout.LabelField(timeFormatted, EditorStyles.miniLabel, GUILayout.Width(45));
                timeElement.floatValue = EditorGUILayout.FloatField(currentVal);

                EditorGUILayout.EndHorizontal();
            }

            // Option to add a new time
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Time Slot"))
            {
                threeStarTimes.Add(0f);
            }
            if (GUILayout.Button("Remove Last Time Slot"))
            {
                if (threeStarTimes.Count > 0)
                    threeStarTimes.RemoveAt(threeStarTimes.Count - 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }
        // ==========================================

        EditorGUILayout.Space();
        if (GUILayout.Button("Fix All Scenes"))
        {
            FixScenes();
        }
        so.ApplyModifiedProperties();
    }

    // Helper method to parse text from Excel/CSV
    // Helper method to parse text from Excel (Handles mm:ss format)
    private void ParseTimesFromText()
    {
        if (string.IsNullOrEmpty(pastedTimesText)) return;

        threeStarTimes.Clear();

        // Split by newline, carriage return, or comma
        string[] lines = Regex.Split(pastedTimesText, @"[\n\r,]+");
        int parsedCount = 0;

        foreach (string line in lines)
        {
            string cleanLine = line.Trim();
            if (string.IsNullOrEmpty(cleanLine)) continue;

            if (cleanLine.Contains(":"))
            {
                // Format is mm:ss (e.g., "00:05" or "01:30")
                string[] parts = cleanLine.Split(':');
                if (parts.Length == 2)
                {
                    if (float.TryParse(parts[0], out float minutes) && float.TryParse(parts[1], out float seconds))
                    {
                        float totalSeconds = (minutes * 60f) + seconds;
                        threeStarTimes.Add(totalSeconds);
                        parsedCount++;
                    }
                }
            }
            else
            {
                // Format is just a number (e.g., "5" or "10.5")
                string normalized = cleanLine.Replace(',', '.');
                if (float.TryParse(normalized, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float seconds))
                {
                    threeStarTimes.Add(seconds);
                    parsedCount++;
                }
            }
        }

        Debug.Log($"✅ Parsed {parsedCount} times from text.");

        // Check if we have enough times for all scenes
        if (scenesToFix.Count > 0 && threeStarTimes.Count < scenesToFix.Count)
        {
            Debug.LogWarning($"⚠️ WARNING: Only {threeStarTimes.Count} times parsed, but you have {scenesToFix.Count} scenes.");
            Debug.LogWarning($"   Scenes {threeStarTimes.Count} through {scenesToFix.Count - 1} will keep their existing LevelGoal times.");

            // Show which scenes are missing times
            for (int i = threeStarTimes.Count; i < Mathf.Min(scenesToFix.Count, 10); i++) // Show first 10 missing
            {
                string sceneName = scenesToFix[i] != null ? scenesToFix[i].name : $"Scene {i}";
                Debug.LogWarning($"   Missing time for scene index {i}: '{sceneName}'");
            }
            if (scenesToFix.Count - threeStarTimes.Count > 10)
            {
                Debug.LogWarning($"   ... and {scenesToFix.Count - threeStarTimes.Count - 10} more scenes missing times.");
            }
        }
        else if (threeStarTimes.Count > scenesToFix.Count && scenesToFix.Count > 0)
        {
            Debug.LogWarning($"⚠️ You parsed {threeStarTimes.Count} times but only have {scenesToFix.Count} scenes.");
            Debug.LogWarning($"   Extra times from index {scenesToFix.Count} onwards will be ignored.");
        }
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
                    }
                }
            }

            // --- Fix LevelGoal + Obstacles + Star Times ---
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
                }

                if (ShouldParentAllObstacles)
                {
                    GameObject obstacleParent = GameObject.Find("Obstacles") ?? new GameObject("Obstacles");
                    obstacleParent.transform.position = Vector3.zero;
                    foreach (Obstacle obs in obstacles)
                        obs.transform.SetParent(obstacleParent.transform, true);
                    modified = true;
                }

                // ==========================================
                // --- NEW: APPLY STAR TIMES ---
                // ==========================================
                if (ShouldFixStarTimes)
                {
                    // Check if we have a time for this scene
                    if (sceneCounter < threeStarTimes.Count)
                    {
                        float targetThreeStar = threeStarTimes[sceneCounter];

                        // Only apply if it's greater than 0 (0 means "not configured")
                        if (targetThreeStar > 0)
                        {
                            // Store original for debug
                            float originalThreeStar = levelGoal.threeStarTime;

                            levelGoal.threeStarTime = targetThreeStar;
                            levelGoal.twoStarTime = Mathf.Ceil(targetThreeStar*2);
                            levelGoal.oneStarTime = defaultOneStarTime;

                            EditorUtility.SetDirty(levelGoal);
                            modified = true;
                            Debug.Log($"✅ Scene '{scene.name}' (index {sceneCounter}): Set 3★={targetThreeStar}s (was {originalThreeStar}s)");
                        }
                        else
                        {
                            Debug.Log($"ℹ️ Scene '{scene.name}' (index {sceneCounter}): Time is 0 (not configured). Keeping existing 3★={levelGoal.threeStarTime}s");
                        }
                    }
                    else
                    {
                        // No time defined for this scene - keep existing values
                        Debug.Log($"ℹ️ Scene '{scene.name}' (index {sceneCounter}): No time defined (only {threeStarTimes.Count} times provided). Keeping existing 3★={levelGoal.threeStarTime}s");
                    }
                }
                // ==========================================
            }
            else
            {
                Debug.LogWarning($"No LevelGoal found in scene {scene.name}");
            }

            // --- Fix Obstacle Weights ---
            GameManager gm = GameObject.FindFirstObjectByType<GameManager>();
            if (ShouldFixObstacleWeights && gm != null)
            {
                float weightToApply = ObstacleWeightModifier;
                if (UseIncrementalWeight && ScenesPerIncrement > 0)
                {
                    int incrementGroup = sceneCounter / ScenesPerIncrement;
                    weightToApply = ObstacleWeightModifier + (incrementGroup * WeightIncrement);
                }
                gm.ObstacleWeightModifier = weightToApply;
                EditorUtility.SetDirty(gm);
                modified = true;
            }

            // --- FIX CAMERA ZOOM ---
            if (ShouldFixDefaultZoom && gm != null)
            {
                gm.defaultZoomValue = DefaultZoomValue;
                EditorUtility.SetDirty(gm);
                modified = true;
            }

            // --- ADD BACK TO MAIN MENU BUTTON ---
            if (AddBackToMainMenuButton)
            {
                if (gm == null) gm = GameObject.FindFirstObjectByType<GameManager>();
                if (gm != null)
                {
                    gm.ShouldHaveMainMenuButton = true;
                    EditorUtility.SetDirty(gm);
                    modified = true;
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