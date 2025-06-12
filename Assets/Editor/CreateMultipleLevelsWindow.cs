// CreateMultipleLevelsWindow.cs (FIXED: Operator '!' cannot be applied to operand of type 'int')
using UnityEngine;
using UnityEditor; 
using System.IO;   

public class CreateMultipleLevelsWindow : EditorWindow
{
    private int numberOfLevelsToCreate = 1; 
    private string outputFolderPath = "Assets/Levels"; 

    [MenuItem("Assets/Create/Scene Data/Multiple Levels...", false, 0)]
    public static void ShowWindow()
    {
        GetWindow<CreateMultipleLevelsWindow>("Create Multiple Levels");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Multiple Level ScriptableObjects", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        numberOfLevelsToCreate = EditorGUILayout.IntField(new GUIContent("Number of Levels", "Enter how many Level ScriptableObjects you want to create."), numberOfLevelsToCreate);
        numberOfLevelsToCreate = Mathf.Max(1, numberOfLevelsToCreate);

        EditorGUILayout.LabelField(new GUIContent("Output Folder (relative to Assets)", "The folder where the new Level assets will be saved. e.g., 'Assets/GameData/Levels'"));
        outputFolderPath = EditorGUILayout.TextField("Folder Path", outputFolderPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Levels"))
        {
            CreateLevels();
        }

        EditorGUILayout.HelpBox("New Level assets will be named 'Level_X.asset' and their 'Level Number' field will be set sequentially.", MessageType.Info);
    }

    private void CreateLevels()
    {
        if (numberOfLevelsToCreate <= 0)
        {
            EditorUtility.DisplayDialog("Input Error", "Please enter a number greater than 0 for levels to create.", "OK");
            return;
        }

        string cleanedFolderPath = outputFolderPath.Replace("\\", "/").Trim();
        if (!cleanedFolderPath.StartsWith("Assets/"))
        {
            cleanedFolderPath = "Assets/" + cleanedFolderPath;
        }
        cleanedFolderPath = cleanedFolderPath.TrimEnd('/');

        string fullSystemPath = Path.Combine(Application.dataPath, cleanedFolderPath.Replace("Assets/", ""));

        if (!Directory.Exists(fullSystemPath))
        {
            Directory.CreateDirectory(fullSystemPath);
            AssetDatabase.Refresh(); 
        }

        int createdCount = 0;
        for (int i = 0; i < numberOfLevelsToCreate; i++)
        {
            int currentLevelNumber = i + 1; 
            Level newLevel = ScriptableObject.CreateInstance<Level>();
            newLevel.levelNumber = currentLevelNumber;

            string assetName = $"Level_{currentLevelNumber}.asset";
            string assetPath = Path.Combine(cleanedFolderPath, assetName);

            if (File.Exists(assetPath))
            {
                // Corrected logic: Store the int result and compare it directly
                int dialogResult = EditorUtility.DisplayDialogComplex(
                    "Asset Exists",
                    $"An asset named '{assetName}' already exists at '{cleanedFolderPath}'. Do you want to overwrite it?",
                    "Overwrite", "Skip", "Cancel");

                if (dialogResult == 1) // "Skip" button was clicked
                {
                    Debug.Log($"Skipping creation of '{assetName}' as it already exists and skip was chosen.");
                    continue; // Skip to the next level in the loop
                }
                else if (dialogResult == 2) // "Cancel" button was clicked
                {
                    Debug.Log($"Cancelling creation process at '{assetName}'.");
                    return; // Stop the entire creation process
                }
                // If dialogResult is 0 ("Overwrite"), the code will proceed to overwrite.
            }

            AssetDatabase.CreateAsset(newLevel, assetPath);
            createdCount++;
            Debug.Log($"Created Level asset: {assetPath} with Level Number: {newLevel.levelNumber}");
        }

        AssetDatabase.SaveAssets(); 
        AssetDatabase.Refresh();    

        EditorUtility.DisplayDialog("Levels Creation Complete", $"Successfully created {createdCount} Level asset(s) in:\n{cleanedFolderPath}", "OK");
        this.Close();
    }
}