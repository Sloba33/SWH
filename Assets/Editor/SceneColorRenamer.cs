#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneColorRenamer : EditorWindow
{
    public List<SceneAsset> scenes = new List<SceneAsset>();

    [MenuItem("Tools/Scene Color Renamer")]
    public static void ShowWindow()
    {
        GetWindow<SceneColorRenamer>("Scene Color Renamer");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);
        SerializedProperty sp = so.FindProperty("scenes");

        EditorGUILayout.PropertyField(sp, true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (GUILayout.Button("Rename Scenes by Obstacle Colors"))
        {
            RenameScenes();
        }
    }

    private void RenameScenes()
    {
        foreach (SceneAsset sceneAsset in scenes)
        {
            if (sceneAsset == null) continue;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            Debug.Log($"[SceneRenamer] Processing scene {sceneName}");

            // Open scene (in single mode but without saving changes)
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var obstacles = GameObject.FindObjectsOfType<Obstacle>(true);

            if (obstacles.Length == 0)
            {
                Debug.LogWarning($"[SceneRenamer] No obstacles found in {sceneName}, skipping.");
                continue;
            }

            List<string> colors = new List<string>();
            foreach (var obs in obstacles)
            {
                if (obs == null) continue;

                string type = obs.obstacleType.ToString();
                string color = obs.obstacleColor.ToString();

                // Ignore Metal types and Default colors
                if (type.Equals("Metal", StringComparison.OrdinalIgnoreCase)) continue;
                if (color.Equals("Default", StringComparison.OrdinalIgnoreCase)) continue;

                if (!string.IsNullOrWhiteSpace(color))
                    colors.Add(color);
            }

            // Deduplicate, normalize, sort
            colors = colors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            colors.Sort(StringComparer.OrdinalIgnoreCase);

            string colorSuffix = string.Join("", colors);

            // Skip if already contains suffix
            if (sceneName.EndsWith("_" + colorSuffix, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[SceneRenamer] Scene {sceneName} already contains suffix, skipping rename.");
                continue;
            }

            string newName = sceneName + "_" + colorSuffix;

            string err = AssetDatabase.RenameAsset(scenePath, newName);
            if (!string.IsNullOrEmpty(err))
                Debug.LogError($"[SceneRenamer] Rename error for {sceneName}: {err}");
            else
                Debug.Log($"[SceneRenamer] Renamed {sceneName} -> {newName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Optional: re-open previous scene
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
    }
}
#endif
