using UnityEditor;
using UnityEngine;
using System.IO; // <-- This is required for Path.GetFileNameWithoutExtension

public class LevelBatchCreator : EditorWindow
{
    public int startingLevelNumber = 1;
    public int amountToCreate = 10;
    public string namePrefix = "Toy Factory - ";
    public int startingDisplayNumber = 1;
    public string saveFolderPath = "Assets/Levels/";

    [MenuItem("Tools/Level Batch Creator")]
    public static void ShowWindow()
    {
        GetWindow<LevelBatchCreator>("Level Batch Creator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Level Creator", EditorStyles.boldLabel);
        startingLevelNumber = EditorGUILayout.IntField("Starting Level Number", startingLevelNumber);
        amountToCreate = EditorGUILayout.IntField("Amount to Create", amountToCreate);
        namePrefix = EditorGUILayout.TextField("Name Prefix", namePrefix);
        startingDisplayNumber = EditorGUILayout.IntField("Starting Display Number", startingDisplayNumber);
        saveFolderPath = EditorGUILayout.TextField("Save Folder Path", saveFolderPath);

        if (GUILayout.Button("Create Levels"))
            CreateLevels();
    }

    private void CreateLevels()
    {
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
            Debug.Log($"📁 Created folder: {saveFolderPath}");
        }

        // ⚠️ Optimization: Get build scenes *once* before the loop.
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

        for (int i = 0; i < amountToCreate; i++)
        {
            int currentLevelNumber = startingLevelNumber + i;
            int displayNumber = startingDisplayNumber + i;

            // 1. Create the instance
            Level newLevel = ScriptableObject.CreateInstance<Level>();

            // 2. Set Level Number
            newLevel.levelNumber = currentLevelNumber;
            
            // 3. Set Build Index (Logic from your OnEnable)
            newLevel.sceneBuildIndex = currentLevelNumber + 2;

            
            // 🔴 === 4. THIS IS THE COPIED LOGIC FROM YOUR LevelEditor.cs === 🔴
            
            string scenePath = null;
            int currentBuildIndex = newLevel.sceneBuildIndex; // Use the index we just set

            // Find the scene path from the build settings
            for (int j = 0; j < buildScenes.Length; j++)
            {
                // Check if the scene is enabled and the index matches
                if (buildScenes[j].enabled && j == currentBuildIndex)
                {
                    scenePath = buildScenes[j].path;
                    break; // Found it, stop looping
                }
            }

            // Set the sceneName based on what was found
            if (!string.IsNullOrEmpty(scenePath))
            {
                newLevel.sceneName = Path.GetFileNameWithoutExtension(scenePath);
            }
            else
            {
                newLevel.sceneName = "N/A (Not in Build Settings)";
                // Log a warning for this specific level
                Debug.LogWarning($"LevelBatchCreator: Level asset for level number {currentLevelNumber}: Scene with build index {currentBuildIndex} is NOT found or enabled in Build Settings.");
            }
            // 🔴 === END OF COPIED LOGIC === 🔴
            

            // 5. Set up file path
            string cleanPrefix = namePrefix.TrimEnd();
            string assetName = $"{cleanPrefix} - {displayNumber}";
            string assetPath = $"{saveFolderPath}/{assetName}.asset";

            // 6. Create the asset
            AssetDatabase.CreateAsset(newLevel, assetPath);
            EditorUtility.SetDirty(newLevel);
            
            // This log will now correctly show all fields
            Debug.Log($"✅ Created: {assetName}  →  LevelNumber: {newLevel.levelNumber}, SceneIndex: {newLevel.sceneBuildIndex}, SceneName: '{newLevel.sceneName}'");
        }

        // 7. Save and refresh ONCE (much faster)
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎯 Successfully created {amountToCreate} levels starting from {startingLevelNumber}");
    }
}