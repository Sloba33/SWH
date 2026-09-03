using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class SceneRenamer : EditorWindow
{
    public string searchPattern = " - \\d+x\\d+_?[A-Z]?$"; // Regex pattern to remove
    public string folderPath = "Assets/Scenes/"; // Where your scenes are
    public string previewResult = "";

    [MenuItem("Tools/Scene Renamer")]
    public static void ShowWindow()
    {
        GetWindow<SceneRenamer>("Scene Renamer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Mass Scene Renamer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("This will rename scene files by removing patterns from their names.");
        EditorGUILayout.Space();

        folderPath = EditorGUILayout.TextField("Scenes Folder Path", folderPath);
        searchPattern = EditorGUILayout.TextField("Pattern to Remove (Regex)", searchPattern);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Preview Changes"))
        {
            PreviewChanges();
        }

        EditorGUILayout.Space();
        
        if (!string.IsNullOrEmpty(previewResult))
        {
            EditorGUILayout.LabelField("Preview Results:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(previewResult, MessageType.Info);
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("APPLY RENAMES", GUILayout.Height(30)))
        {
            ApplyRenames();
        }
        GUI.backgroundColor = Color.white;
    }

    private void PreviewChanges()
    {
        if (!Directory.Exists(folderPath))
        {
            previewResult = $"❌ Folder not found: {folderPath}";
            return;
        }

        var sceneFiles = Directory.GetFiles(folderPath, "*.unity", SearchOption.TopDirectoryOnly);
        if (sceneFiles.Length == 0)
        {
            previewResult = "❌ No .unity files found in the specified folder.";
            return;
        }

        string result = "";
        int changeCount = 0;

        foreach (string filePath in sceneFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string newName = Regex.Replace(fileName, searchPattern, "");

            if (fileName != newName)
            {
                result += $"📝 '{fileName}' → '{newName}'\n";
                changeCount++;
            }
        }

        if (changeCount == 0)
        {
            previewResult = "✅ No files match the pattern. Nothing to rename.";
        }
        else
        {
            previewResult = $"Found {changeCount} files to rename:\n\n{result}";
        }
    }

    private void ApplyRenames()
    {
        if (!Directory.Exists(folderPath))
        {
            EditorUtility.DisplayDialog("Error", $"Folder not found: {folderPath}", "OK");
            return;
        }

        var sceneFiles = Directory.GetFiles(folderPath, "*.unity", SearchOption.TopDirectoryOnly);
        if (sceneFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No .unity files found in the specified folder.", "OK");
            return;
        }

        // Confirm with user
        if (!EditorUtility.DisplayDialog("Confirm Rename", 
            $"Are you sure you want to rename files in:\n{folderPath}\n\nThis cannot be undone!", 
            "Yes, Rename Files", "Cancel"))
        {
            return;
        }

        int renamedCount = 0;
        string renamedList = "";

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string filePath in sceneFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string newName = Regex.Replace(fileName, searchPattern, "");
                string newFilePath = Path.Combine(folderPath, newName + ".unity");

                if (fileName != newName && !File.Exists(newFilePath))
                {
                    // Rename the file
                    File.Move(filePath, newFilePath);
                    renamedCount++;
                    renamedList += $"✅ '{fileName}.unity' → '{newName}.unity'\n";
                }
                else if (File.Exists(newFilePath) && fileName != newName)
                {
                    renamedList += $"⚠️ Skipped '{fileName}.unity' - Target name already exists\n";
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        // Refresh AssetDatabase to detect changes
        AssetDatabase.Refresh();

        string message = $"✅ Successfully renamed {renamedCount} files.\n\n{renamedList}";
        EditorUtility.DisplayDialog("Rename Complete", message, "OK");
        Debug.Log($"🔧 Scene Renamer: {message}");
        
        // Update preview
        previewResult = message;
    }
}