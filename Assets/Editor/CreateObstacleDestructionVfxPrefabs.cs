using System.Collections.Generic;
using System.IO;
using Coherence.Editor;
using Coherence.Toolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class CreateObstacleDestructionVfxPrefabs : EditorWindow
{
    private DefaultAsset folder;
    private SceneAsset sceneAsset;

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

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Scene (wire in-scene obstacles)", EditorStyles.boldLabel);
        sceneAsset = (SceneAsset)EditorGUILayout.ObjectField("Scene", sceneAsset, typeof(SceneAsset), false);

        string scenePath = sceneAsset ? AssetDatabase.GetAssetPath(sceneAsset) : null;
        bool sceneValid = !string.IsNullOrEmpty(scenePath);

        using (new EditorGUI.DisabledScope(!sceneValid))
        {
            if (GUILayout.Button("Create VFX Prefabs and Wire Scene Obstacles"))
            {
                RunScene(scenePath);
            }
        }

        EditorGUILayout.HelpBox(
            "Scene mode only touches obstacles that are plain scene objects. Obstacles that are " +
            "prefab instances are left alone — wire those via their prefab in the folder above.",
            MessageType.Info);
    }

    private class RunStats
    {
        public int vfxCreated;
        public int vfxReused;
        public int wired;
        public int prefabInstancesSkipped;
        public int skipped;
    }

    private static void Run(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        var obstacleToVfx = new Dictionary<string, string>();
        var createdVfxPaths = new List<string>();
        var stats = new RunStats();

        foreach (var guid in guids)
        {
            string obstaclePath = AssetDatabase.GUIDToAssetPath(guid);
            var obstacleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(obstaclePath);
            if (obstacleAsset == null) continue;

            var obstacleComp = obstacleAsset.GetComponent<Obstacle>();
            if (obstacleComp == null) continue;

            string vfxPath = ResolveVfxPrefab(obstacleComp.destructionParticleSystem, obstacleAsset.name,
                createdVfxPaths, stats);
            if (vfxPath != null)
                obstacleToVfx[obstaclePath] = vfxPath;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CreateConfigsForVfxPrefabs(createdVfxPaths);

        foreach (var kvp in obstacleToVfx)
        {
            if (WireObstaclePrefab(kvp.Key, kvp.Value))
                stats.wired++;
            else
                stats.skipped++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BakeIfNeeded(createdVfxPaths);

        Debug.Log($"[ObstacleVfx] Done. VFX created: {stats.vfxCreated}, reused: {stats.vfxReused}, " +
                  $"obstacles wired: {stats.wired}, skipped: {stats.skipped}.");
    }

    private static void RunScene(string scenePath)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"[ObstacleVfx] Could not open scene at {scenePath}.");
            return;
        }

        var obstacles = new List<Obstacle>();
        foreach (var root in scene.GetRootGameObjects())
            obstacles.AddRange(root.GetComponentsInChildren<Obstacle>(true));

        var sceneObstacleToVfx = new List<(Obstacle obstacle, string vfxPath)>();
        var createdVfxPaths = new List<string>();
        var stats = new RunStats();

        foreach (var obstacle in obstacles)
        {
            // Prefab instances are wired through their prefab asset by the folder pass —
            // editing them here would just create per-instance overrides.
            if (PrefabUtility.IsPartOfPrefabInstance(obstacle.gameObject))
            {
                stats.prefabInstancesSkipped++;
                continue;
            }

            string vfxPath = ResolveVfxPrefab(obstacle.destructionParticleSystem, obstacle.name,
                createdVfxPaths, stats);
            if (vfxPath != null)
                sceneObstacleToVfx.Add((obstacle, vfxPath));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CreateConfigsForVfxPrefabs(createdVfxPaths);

        foreach (var (obstacle, vfxPath) in sceneObstacleToVfx)
        {
            if (WireSceneObstacle(obstacle, vfxPath))
                stats.wired++;
            else
                stats.skipped++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BakeIfNeeded(createdVfxPaths);

        Debug.Log($"[ObstacleVfx] Scene done ({Path.GetFileNameWithoutExtension(scenePath)}). " +
                  $"VFX created: {stats.vfxCreated}, reused: {stats.vfxReused}, obstacles wired: {stats.wired}, " +
                  $"prefab instances skipped: {stats.prefabInstancesSkipped}, skipped: {stats.skipped}.");
    }

    // Resolves (creating if needed) the networked VFX prefab for one obstacle's
    // destructionParticleSystem. Returns the prefab path, or null if it couldn't be resolved.
    private static string ResolveVfxPrefab(ParticleSystem originalPs, string contextName,
        List<string> createdVfxPaths, RunStats stats)
    {
        if (originalPs == null)
        {
            Debug.LogWarning($"[ObstacleVfx] {contextName}: destructionParticleSystem is not assigned. Skipping.");
            stats.skipped++;
            return null;
        }

        string originalPsPath = AssetDatabase.GetAssetPath(originalPs);
        if (string.IsNullOrEmpty(originalPsPath))
        {
            Debug.LogWarning($"[ObstacleVfx] {contextName}: destructionParticleSystem is not a prefab asset. Skipping.");
            stats.skipped++;
            return null;
        }

        string vfxPath = ComputeVfxPath(originalPsPath);

        if (createdVfxPaths.Contains(vfxPath))
            return vfxPath;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath) != null)
        {
            Debug.Log($"[ObstacleVfx] VFX prefab already exists at {vfxPath} — reusing.");
            stats.vfxReused++;
            return vfxPath;
        }

        if (CreateVfxPrefab(originalPsPath, vfxPath))
        {
            createdVfxPaths.Add(vfxPath);
            stats.vfxCreated++;
            return vfxPath;
        }

        stats.skipped++;
        return null;
    }

    private static void CreateConfigsForVfxPrefabs(List<string> createdVfxPaths)
    {
        foreach (var vfxPath in createdVfxPaths)
        {
            var assetOnDisk = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);
            if (assetOnDisk != null)
                CoherenceSyncConfigUtils.Create(assetOnDisk);
        }
    }

    private static void BakeIfNeeded(List<string> createdVfxPaths)
    {
        if (createdVfxPaths.Count == 0) return;

        Debug.Log("[ObstacleVfx] Running Coherence bake...");
        bool baked = BakeUtil.Bake();
        Debug.Log(baked
            ? "[ObstacleVfx] Bake succeeded."
            : "[ObstacleVfx] Bake failed — check Console and bake manually via Coherence menu.");
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
        var vfxComponent = LoadVfxComponent(vfxPath);
        if (vfxComponent == null)
            return false;

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

    private static bool WireSceneObstacle(Obstacle obstacle, string vfxPath)
    {
        var vfxComponent = LoadVfxComponent(vfxPath);
        if (vfxComponent == null)
            return false;

        obstacle.obstacleDestructionVfxPrefab = vfxComponent;
        EditorUtility.SetDirty(obstacle);
        Debug.Log($"[ObstacleVfx] Wired VFX into scene obstacle {obstacle.name}.");
        return true;
    }

    private static ObstacleDestructionVfx LoadVfxComponent(string vfxPath)
    {
        var vfxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);
        if (vfxAsset == null)
        {
            Debug.LogError($"[ObstacleVfx] Could not load VFX prefab at {vfxPath}.");
            return null;
        }

        var vfxComponent = vfxAsset.GetComponent<ObstacleDestructionVfx>();
        if (vfxComponent == null)
            Debug.LogError($"[ObstacleVfx] VFX prefab {vfxPath} has no ObstacleDestructionVfx component.");

        return vfxComponent;
    }
}
