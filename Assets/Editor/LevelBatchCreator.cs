using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

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

        // Get build scenes just to validate
        var buildScenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .ToList();

        if (buildScenes.Count == 0)
        {
            Debug.LogError("❌ No enabled scenes found in Build Settings!");
            return;
        }

        for (int i = 0; i < amountToCreate; i++)
        {
            int currentLevelNumber = startingLevelNumber + i;
            int displayNumber = startingDisplayNumber + i;

            // Create the instance
            Level newLevel = ScriptableObject.CreateInstance<Level>();

            // Set Level Number
            newLevel.levelNumber = currentLevelNumber;
            
            // Set build index directly: level number + 2
            int targetBuildIndex = currentLevelNumber + 2;
            newLevel.sceneBuildIndex = targetBuildIndex;

            // Verify the scene exists at that index
            if (targetBuildIndex < buildScenes.Count)
            {
                string scenePath = buildScenes[targetBuildIndex].path;
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                Debug.Log($"✅ Level {currentLevelNumber} → Scene '{sceneName}' at build index {targetBuildIndex}");
            }
            else
            {
                Debug.LogError($"❌ Level {currentLevelNumber}: No scene at build index {targetBuildIndex}! Only {buildScenes.Count} scenes in build settings.");
            }

            // Setup file path
            string cleanPrefix = namePrefix.TrimEnd();
            string assetName = $"{cleanPrefix} - {displayNumber}";
            string assetPath = $"{saveFolderPath}/{assetName}.asset";

            // Create the asset
            AssetDatabase.CreateAsset(newLevel, assetPath);
            EditorUtility.SetDirty(newLevel);
            
            Debug.Log($"✅ Created: {assetName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"🎯 Successfully created {amountToCreate} levels starting from {startingLevelNumber}");
    }
}