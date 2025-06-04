// LevelEditor.cs (Updated with Diagnostic Logs)
using UnityEngine;
using UnityEditor; 
using System.IO;   

[CustomEditor(typeof(Level))]
public class LevelEditor : Editor
{
    private SerializedProperty levelNumberProp;
    private SerializedProperty sceneBuildIndexProp;
    private SerializedProperty sceneNameProp;

    private void OnEnable()
    {
        levelNumberProp = serializedObject.FindProperty("levelNumber");
        if (levelNumberProp == null) Debug.LogError("LevelEditor: Could not find 'levelNumber' property in Level.cs!");

        sceneBuildIndexProp = serializedObject.FindProperty("sceneBuildIndex");
        if (sceneBuildIndexProp == null) Debug.LogError("LevelEditor: Could not find 'sceneBuildIndex' property in Level.cs!");

        sceneNameProp = serializedObject.FindProperty("sceneName");
        if (sceneNameProp == null) Debug.LogError("LevelEditor: Could not find 'sceneName' property in Level.cs!");
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject == null || serializedObject.targetObject == null)
        {
            EditorGUILayout.HelpBox("Serialized object is null. Please ensure Level asset is properly created and assigned.", MessageType.Error);
            return;
        }

        serializedObject.Update();

        if (levelNumberProp != null)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(levelNumberProp, new GUIContent("Level Number"));
            if (EditorGUI.EndChangeCheck())
            {
                if (sceneBuildIndexProp != null)
                {
                    sceneBuildIndexProp.intValue = levelNumberProp.intValue + 2;
                }
                
                string scenePath = null;
                int currentBuildIndex = sceneBuildIndexProp.intValue; // Get the calculated index
                
                // --- Diagnostic Log 1: Show the index being looked for ---
                Debug.Log($"LevelEditor: Looking for scene at build index {currentBuildIndex} for Level Number {levelNumberProp.intValue}.");

                for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                {
                    if (EditorBuildSettings.scenes[i].enabled && i == currentBuildIndex)
                    {
                        scenePath = EditorBuildSettings.scenes[i].path;
                        break; 
                    }
                }

                if (sceneNameProp != null)
                {
                    if (!string.IsNullOrEmpty(scenePath))
                    {
                        string derivedSceneName = Path.GetFileNameWithoutExtension(scenePath);
                        sceneNameProp.stringValue = derivedSceneName;
                        // --- Diagnostic Log 2: Show what scene name was derived ---
                        Debug.Log($"LevelEditor: Found scene path '{scenePath}', derived name '{derivedSceneName}'. Setting sceneName to '{derivedSceneName}'.");
                    }
                    else
                    {
                        sceneNameProp.stringValue = "N/A (Not in Build Settings)";
                        // --- Diagnostic Log 3: Show when scene is not found ---
                        Debug.LogWarning($"LevelEditor: Level asset '{target.name}': Scene with build index {currentBuildIndex} is NOT found or enabled in Build Settings. Please add it to 'File > Build Settings > Scenes In Build'.");
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Level Number property not found. Please check Level.cs for 'public int levelNumber;'.", MessageType.Error);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Configuration (Auto-calculated)", EditorStyles.boldLabel);
        
        GUI.enabled = false; 

        if (sceneBuildIndexProp != null)
        {
            EditorGUILayout.PropertyField(sceneBuildIndexProp, new GUIContent("Build Index"));
        }
        else
        {
            EditorGUILayout.HelpBox("Build Index property not found. Please check Level.cs for 'public int sceneBuildIndex;'.", MessageType.Error);
        }

        if (sceneNameProp != null)
        {
            EditorGUILayout.PropertyField(sceneNameProp, new GUIContent("Scene Name"));
        }
        else
        {
            EditorGUILayout.HelpBox("Scene Name property not found. Please check Level.cs for 'public string sceneName;'.", MessageType.Error);
        }
        
        GUI.enabled = true; 

        serializedObject.ApplyModifiedProperties();
    }
}