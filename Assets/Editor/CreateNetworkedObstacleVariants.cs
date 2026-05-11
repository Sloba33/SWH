using System.Collections.Generic;
using System.IO;
using Coherence.Editor;
using Coherence.Toolkit;
using UnityEditor;
using UnityEngine;

public class CreateNetworkedObstacleVariants : EditorWindow
{
    private DefaultAsset folder;

    [MenuItem("Tools/Create Networked Obstacle Variants")]
    public static void ShowWindow()
    {
        GetWindow<CreateNetworkedObstacleVariants>("Networked Obstacles");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Source Folder", EditorStyles.boldLabel);
        folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", folder, typeof(DefaultAsset), false);

        string folderPath = folder ? AssetDatabase.GetAssetPath(folder) : null;
        bool isValid = !string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath);

        using (new EditorGUI.DisabledScope(!isValid))
        {
            if (GUILayout.Button("Create Networked Variants"))
            {
                Run(folderPath);
            }
        }

        if (folder != null && !isValid)
        {
            EditorGUILayout.HelpBox("Selected asset is not a folder.", MessageType.Warning);
        }
    }

    private static void Run(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        int created = 0, skipped = 0, ignored = 0;
        var createdPaths = new List<string>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (sourceAsset == null) continue;

            if (sourceAsset.GetComponent<Obstacle>() == null)
            {
                ignored++;
                continue;
            }

            if (sourceAsset.GetComponent<CoherenceSync>() != null)
            {
                ignored++;
                continue;
            }

            string dir = Path.GetDirectoryName(path).Replace("\\", "/");
            string name = Path.GetFileNameWithoutExtension(path);
            string outputPath = $"{dir}/{name}_Networked.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
            {
                Debug.Log($"[NetworkedObstacles] Skipping {name}Networked — already exists.");
                skipped++;
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset);
            try
            {
                var sync = instance.AddComponent<CoherenceSync>();
                sync.uniquenessType = CoherenceSync.UniquenessType.NoDuplicates;

                CoherenceSyncUtils.AddBinding<Transform>(instance, "position");
                CoherenceSyncUtils.AddBinding<Transform>(instance, "rotation");
                CoherenceSyncUtils.AddBinding<Obstacle>(instance, "isBeingPushed");
                CoherenceSyncUtils.AddBinding<Obstacle>(instance, "isBeingPulled");
                CoherenceSyncUtils.AddBinding<Obstacle>(instance, "isFalling");

                PrefabUtility.SaveAsPrefabAsset(instance, outputPath, out bool success);
                if (success)
                {
                    createdPaths.Add(outputPath);
                    Debug.Log($"[NetworkedObstacles] Created variant: {outputPath}");
                    created++;
                }
                else
                {
                    Debug.LogError($"[NetworkedObstacles] Failed to save variant: {outputPath}");
                }
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Register configs against freshly-loaded asset references so EditorTarget points at the
        // on-disk prefab (the GameObject SaveAsPrefabAsset returns can become stale after the scene
        // instance is destroyed, leaving a dangling reference that breaks the subsequent bake).
        foreach (var outputPath in createdPaths)
        {
            var assetOnDisk = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (assetOnDisk != null)
                CoherenceSyncConfigUtils.Create(assetOnDisk);
        }

        if (createdPaths.Count > 0)
        {
            Debug.Log("[NetworkedObstacles] Running Coherence bake...");
            bool baked = BakeUtil.Bake();
            Debug.Log(baked
                ? "[NetworkedObstacles] Bake succeeded."
                : "[NetworkedObstacles] Bake failed — check the Console for Coherence errors and bake manually via Coherence menu.");
        }

        Debug.Log($"[NetworkedObstacles] Done. Created: {created}, skipped (existed): {skipped}, ignored (not Obstacle root or already networked): {ignored}.");
    }
}