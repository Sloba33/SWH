using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

public class LevelDataUpdater : EditorWindow
{
    [MenuItem("Tools/Fix All Level SOs")]
    public static void FixAllLevels()
    {
        var allLevels = AssetDatabase.FindAssets("t:Level")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Select(path => AssetDatabase.LoadAssetAtPath<Level>(path))
            .Where(level => level != null);
        
        int fixedCount = 0;
        
        // Get current build settings
        var buildScenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .ToList();
        
        foreach (var level in allLevels)
        {
            int correctIndex = level.levelNumber + 2;
            bool changed = false;
            
            // Fix the build index
            if (level.sceneBuildIndex != correctIndex)
            {
                level.sceneBuildIndex = correctIndex;
                changed = true;
            }
            
            // Update the scene name from the build index
            if (correctIndex >= 0 && correctIndex < buildScenes.Count)
            {
                string correctSceneName = Path.GetFileNameWithoutExtension(buildScenes[correctIndex].path);
                if (level.sceneName != correctSceneName)
                {
                    level.sceneName = correctSceneName;
                    changed = true;
                }
            }
            else
            {
                level.sceneName = "Scene Not Found";
                changed = true;
            }
            
            if (changed)
            {
                EditorUtility.SetDirty(level);
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"✅ Fixed {fixedCount} levels!");
        }
        else
        {
            Debug.Log("✅ All levels already correct!");
        }
    }
}