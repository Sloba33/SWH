using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(ImageGallery))]
public class ImageGalleryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ImageGallery imageGallery = (ImageGallery)target;

        if (GUILayout.Button("Fill Level Progress Prefabs"))
        {
            FillPrefabs(imageGallery);
        }
    }

    private void FillPrefabs(ImageGallery imageGallery)
    {
        if (imageGallery == null)
        {
            Debug.LogError("ImageGallery reference is null.");
            return;
        }

        serializedObject.Update();

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        if (buildScenes == null || buildScenes.Length == 0)
        {
            Debug.LogError("No scenes in the build settings.");
            return;
        }

        int minIndex = Mathf.Clamp(imageGallery.minBuildIndex, 0, buildScenes.Length - 1);
        int maxIndex = Mathf.Clamp(imageGallery.maxBuildIndex, minIndex, buildScenes.Length - 1);

        HashSet<string> uniquePrefabGuids = new HashSet<string>();
        List<LevelProgress> foundPrefabs = new List<LevelProgress>();

        for (int i = minIndex; i <= maxIndex; i++)
        {
            if (!buildScenes[i].enabled)
            {
                Debug.LogWarning($"[SKIPPED] Scene index {i}: Disabled in build settings.");
                continue;
            }

            string scenePath = buildScenes[i].path;

            // 🔥 Load additively without touching current scene
            Scene tempScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            if (!tempScene.IsValid())
            {
                Debug.LogError($"[ERROR] Failed to load scene at: {scenePath}");
                continue;
            }

            Debug.Log($"[LOADED] {tempScene.name}");

            LevelGoal levelGoal = Object.FindAnyObjectByType<LevelGoal>(FindObjectsInactive.Include);
            if (levelGoal == null)
            {
                Debug.LogWarning($"[SKIPPED] No LevelGoal in '{tempScene.name}'");
                EditorSceneManager.CloseScene(tempScene, false);
                continue;
            }

            LevelProgress lp = levelGoal.levelProgress;
            if (lp == null)
            {
                Debug.LogWarning($"[SKIPPED] LevelGoal in '{tempScene.name}' has no LevelProgress reference.");
                EditorSceneManager.CloseScene(tempScene, false);
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(lp);
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[SKIPPED] LevelProgress in '{tempScene.name}' is NOT a prefab asset!");
                EditorSceneManager.CloseScene(tempScene, false);
                continue;
            }

            if (uniquePrefabGuids.Contains(guid))
            {
                Debug.Log($"[DUPLICATE] '{lp.name}' from scene '{tempScene.name}'");
            }
            else
            {
                uniquePrefabGuids.Add(guid);
                foundPrefabs.Add(lp);
                Debug.Log($"[ADDED] '{lp.name}' from scene '{tempScene.name}'");
            }

            // 🔥 Close temporary scene without saving
            EditorSceneManager.CloseScene(tempScene, false);
        }

        // ---- SAVE RESULTS ----
        SerializedProperty arrayProp = serializedObject.FindProperty("levelProgressPrefabs");
        arrayProp.ClearArray();

        for (int i = 0; i < foundPrefabs.Count; i++)
        {
            arrayProp.InsertArrayElementAtIndex(i);
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = foundPrefabs[i];
        }

        serializedObject.ApplyModifiedProperties(); // 🔥 NOW IT SAVES

        Debug.Log($"=== Completed === Added {foundPrefabs.Count} unique prefabs.");
    }
}
