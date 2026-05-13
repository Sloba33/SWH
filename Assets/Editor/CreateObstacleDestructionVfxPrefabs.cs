using System.Collections.Generic;
using System.IO;
using Coherence.Editor;
using Coherence.Toolkit;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class CreateObstacleDestructionVfxPrefabs : EditorWindow
{
    private DefaultAsset folder;

    [MenuItem("Tools/Create Obstacle Destruction VFX Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<CreateObstacleDestructionVfxPrefabs>("Obstacle Destruction VFX");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Source Folder (obstacle prefabs)", EditorStyles.boldLabel);
        folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", folder, typeof(DefaultAsset), false);

        string folderPath = folder ? AssetDatabase.GetAssetPath(folder) : null;
        bool isValid = !string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath);

        using (new EditorGUI.DisabledScope(!isValid))
        {
            if (GUILayout.Button("Create VFX Prefabs and Wire Obstacles"))
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

        var obstacleToVfx = new Dictionary<string, string>();
        var createdVfxPaths = new List<string>();
        int vfxCreatedCount = 0;
        int vfxReusedCount = 0;
        int skippedCount = 0;

        foreach (var guid in guids)
        {
            string obstaclePath = AssetDatabase.GUIDToAssetPath(guid);
            var obstacleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(obstaclePath);
            if (obstacleAsset == null) continue;

            var obstacleComp = obstacleAsset.GetComponent<Obstacle>();
            if (obstacleComp == null) continue;

            ParticleSystem originalPs = obstacleComp.destructionParticleSystem;
            if (originalPs == null)
            {
                Debug.LogWarning($"[ObstacleVfx] {obstacleAsset.name}: destructionParticleSystem is not assigned. Skipping.");
                skippedCount++;
                continue;
            }

            string originalPsPath = AssetDatabase.GetAssetPath(originalPs);
            if (string.IsNullOrEmpty(originalPsPath))
            {
                Debug.LogWarning($"[ObstacleVfx] {obstacleAsset.name}: destructionParticleSystem is not a prefab asset. Skipping.");
                skippedCount++;
                continue;
            }

            string vfxPath = ComputeVfxPath(originalPsPath);

            if (createdVfxPaths.Contains(vfxPath))
            {
                obstacleToVfx[obstaclePath] = vfxPath;
            }
            else if (AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath) != null)
            {
                Debug.Log($"[ObstacleVfx] VFX prefab already exists at {vfxPath} — reusing.");
                vfxReusedCount++;
                obstacleToVfx[obstaclePath] = vfxPath;
            }
            else if (CreateVfxPrefab(originalPsPath, vfxPath))
            {
                createdVfxPaths.Add(vfxPath);
                vfxCreatedCount++;
                obstacleToVfx[obstaclePath] = vfxPath;
            }
            else
            {
                skippedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        foreach (var vfxPath in createdVfxPaths)
        {
            var assetOnDisk = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);
            if (assetOnDisk != null)
                CoherenceSyncConfigUtils.Create(assetOnDisk);
        }

        int wiredCount = 0;
        foreach (var kvp in obstacleToVfx)
        {
            if (WireObstaclePrefab(kvp.Key, kvp.Value))
                wiredCount++;
            else
                skippedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (createdVfxPaths.Count > 0)
        {
            Debug.Log("[ObstacleVfx] Running Coherence bake...");
            bool baked = BakeUtil.Bake();
            Debug.Log(baked
                ? "[ObstacleVfx] Bake succeeded."
                : "[ObstacleVfx] Bake failed — check Console and bake manually via Coherence menu.");
        }

        Debug.Log($"[ObstacleVfx] Done. VFX created: {vfxCreatedCount}, reused: {vfxReusedCount}, obstacles wired: {wiredCount}, skipped: {skippedCount}.");
    }

    private static string ComputeVfxPath(string originalPsPath)
    {
        string dir = Path.GetDirectoryName(originalPsPath).Replace("\\", "/");
        string name = Path.GetFileNameWithoutExtension(originalPsPath);
        return $"{dir}/{name}_Networked.prefab";
    }

    private static bool CreateVfxPrefab(string originalPsPath, string outputPath)
    {
        var sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(originalPsPath);
        if (sourceAsset == null)
        {
            Debug.LogError($"[ObstacleVfx] Could not load source PS prefab at {originalPsPath}.");
            return false;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset);
        try
        {
            if (instance.GetComponent<ObstacleDestructionVfx>() == null)
                instance.AddComponent<ObstacleDestructionVfx>();

            if (instance.GetComponent<CoherenceSync>() == null)
            {
                instance.AddComponent<CoherenceSync>();
                CoherenceSyncUtils.AddBinding(instance, typeof(Transform), "position");
                CoherenceSyncUtils.AddBinding(instance, typeof(Transform), "rotation");
            }

            PrefabUtility.SaveAsPrefabAsset(instance, outputPath, out bool success);
            if (success)
            {
                Debug.Log($"[ObstacleVfx] Created VFX prefab: {outputPath}");
                return true;
            }

            Debug.LogError($"[ObstacleVfx] Failed to save VFX prefab: {outputPath}");
            return false;
        }
        finally
        {
            if (instance != null) Object.DestroyImmediate(instance);
        }
    }

    private static bool WireObstaclePrefab(string obstaclePath, string vfxPath)
    {
        var vfxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);
        if (vfxAsset == null)
        {
            Debug.LogError($"[ObstacleVfx] Could not load VFX prefab at {vfxPath}.");
            return false;
        }

        var vfxComponent = vfxAsset.GetComponent<ObstacleDestructionVfx>();
        if (vfxComponent == null)
        {
            Debug.LogError($"[ObstacleVfx] VFX prefab {vfxPath} has no ObstacleDestructionVfx component.");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(obstaclePath);
        try
        {
            var obstacle = root.GetComponent<Obstacle>();
            if (obstacle == null)
            {
                Debug.LogError($"[ObstacleVfx] {obstaclePath} has no Obstacle component on root.");
                return false;
            }

            obstacle.obstacleDestructionVfxPrefab = vfxComponent;
            EditorUtility.SetDirty(obstacle);

            PrefabUtility.SaveAsPrefabAsset(root, obstaclePath, out bool success);
            if (success)
            {
                Debug.Log($"[ObstacleVfx] Wired VFX into {Path.GetFileNameWithoutExtension(obstaclePath)}.");
                return true;
            }

            Debug.LogError($"[ObstacleVfx] Failed to save obstacle prefab: {obstaclePath}");
            return false;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}