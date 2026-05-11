using System;
using System.Collections.Generic;
using System.IO;
using Coherence.Editor;
using Coherence.Toolkit;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class CreateNetworkedPrefabVariants : EditorWindow
{
    private DefaultAsset folder;

    private class ConversionDescriptor
    {
        public string Label;
        public Type RootComponent;
        public (Type Component, string Member)[] Bindings;
        // Optional. Runs on the variant instance after bindings are added, before SaveAsPrefabAsset.
        public Action<GameObject> PostProcess;
    }

    // Order matters: descriptors that depend on other variants (e.g. BombCollectible needs the
    // networked Bomb) must come AFTER their dependencies so the dependency is processed first
    // within a single run.
    private static readonly ConversionDescriptor[] Conversions =
    {
        new ConversionDescriptor
        {
            Label = "Obstacle",
            RootComponent = typeof(Obstacle),
            Bindings = new (Type, string)[]
            {
                (typeof(Transform), "position"),
                (typeof(Transform), "rotation"),
                (typeof(Obstacle), "isBeingPushed"),
                (typeof(Obstacle), "isBeingPulled"),
                (typeof(Obstacle), "isFalling"),
            },
        },
        new ConversionDescriptor
        {
            Label = "Bomb",
            RootComponent = typeof(Bomb),
            Bindings = new (Type, string)[]
            {
                (typeof(Transform), "position"),
                (typeof(Bomb), "isColored"),
                (typeof(Bomb), "bombColor"),
                (typeof(Bomb), "time"),
            },
        },
        new ConversionDescriptor
        {
            Label = "PowerupCollectible",
            RootComponent = typeof(PowerupCollectible),
            Bindings = new (Type, string)[]
            {
                (typeof(Transform), "position"),
                (typeof(Transform), "rotation"),
            },
        },
        new ConversionDescriptor
        {
            Label = "BombCollectible",
            RootComponent = typeof(BombCollectible),
            Bindings = new (Type, string)[]
            {
                (typeof(Transform), "position"),
            },
            PostProcess = RewireBombCollectibleToNetworkedBomb,
        },
    };

    private static void RewireBombCollectibleToNetworkedBomb(GameObject instance)
    {
        var collectible = instance.GetComponent<BombCollectible>();
        if (collectible == null) return;

        Bomb originalBomb = collectible.bombPrefab;
        if (originalBomb == null)
        {
            Debug.LogWarning($"[NetworkedPrefabs] BombCollectible '{instance.name}' has no bombPrefab assigned; cannot rewire to networked Bomb.");
            return;
        }

        string bombPath = AssetDatabase.GetAssetPath(originalBomb);
        if (string.IsNullOrEmpty(bombPath))
        {
            Debug.LogError($"[NetworkedPrefabs] BombCollectible '{instance.name}': could not resolve asset path for its bombPrefab.");
            return;
        }

        string dir = Path.GetDirectoryName(bombPath).Replace("\\", "/");
        string name = Path.GetFileNameWithoutExtension(bombPath);
        string networkedBombPath = $"{dir}/{name}_Networked.prefab";

        var networkedBombAsset = AssetDatabase.LoadAssetAtPath<GameObject>(networkedBombPath);
        if (networkedBombAsset == null)
        {
            Debug.LogError($"[NetworkedPrefabs] BombCollectible '{instance.name}': networked Bomb not found at '{networkedBombPath}'. Create the Bomb's networked variant first, then re-run.");
            return;
        }

        var networkedBomb = networkedBombAsset.GetComponent<Bomb>();
        if (networkedBomb == null)
        {
            Debug.LogError($"[NetworkedPrefabs] BombCollectible '{instance.name}': asset at '{networkedBombPath}' has no Bomb component on root.");
            return;
        }

        collectible.bombPrefab = networkedBomb;
    }

    [MenuItem("Tools/Create Networked Prefab Variants")]
    public static void ShowWindow()
    {
        GetWindow<CreateNetworkedPrefabVariants>("Networked Prefabs");
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

        // Pre-bucket prefabs by their matching descriptor so we can process them in the order
        // declared in Conversions (dependencies first).
        var work = new List<(string path, ConversionDescriptor descriptor)>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (sourceAsset == null) continue;

            if (sourceAsset.GetComponent<CoherenceSync>() != null)
            {
                ignored++;
                continue;
            }

            ConversionDescriptor descriptor = null;
            foreach (var c in Conversions)
            {
                if (sourceAsset.GetComponent(c.RootComponent) != null)
                {
                    descriptor = c;
                    break;
                }
            }

            if (descriptor == null)
            {
                ignored++;
                continue;
            }

            work.Add((path, descriptor));
        }

        work.Sort((a, b) => Array.IndexOf(Conversions, a.descriptor) - Array.IndexOf(Conversions, b.descriptor));

        foreach (var (path, descriptor) in work)
        {
            string dir = Path.GetDirectoryName(path).Replace("\\", "/");
            string name = Path.GetFileNameWithoutExtension(path);
            string outputPath = $"{dir}/{name}_Networked.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
            {
                Debug.Log($"[NetworkedPrefabs] Skipping {name}_Networked — already exists.");
                skipped++;
                continue;
            }

            var sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (sourceAsset == null) continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset);
            try
            {
                var sync = instance.AddComponent<CoherenceSync>();
                sync.uniquenessType = CoherenceSync.UniquenessType.NoDuplicates;

                foreach (var binding in descriptor.Bindings)
                {
                    CoherenceSyncUtils.AddBinding(instance, binding.Component, binding.Member);
                }

                descriptor.PostProcess?.Invoke(instance);

                PrefabUtility.SaveAsPrefabAsset(instance, outputPath, out bool success);
                if (success)
                {
                    createdPaths.Add(outputPath);
                    Debug.Log($"[NetworkedPrefabs] Created {descriptor.Label} variant: {outputPath}");
                    created++;
                }
                else
                {
                    Debug.LogError($"[NetworkedPrefabs] Failed to save variant: {outputPath}");
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
            Debug.Log("[NetworkedPrefabs] Running Coherence bake...");
            bool baked = BakeUtil.Bake();
            Debug.Log(baked
                ? "[NetworkedPrefabs] Bake succeeded."
                : "[NetworkedPrefabs] Bake failed — check the Console for Coherence errors and bake manually via Coherence menu.");
        }

        Debug.Log($"[NetworkedPrefabs] Done. Created: {created}, skipped (existed): {skipped}, ignored (no matching root component or already networked): {ignored}.");
    }
}