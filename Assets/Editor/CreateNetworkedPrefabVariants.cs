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
            var sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (sourceAsset == null) continue;

            var variant = CreateVariant(sourceAsset, descriptor.Bindings, descriptor.PostProcess, descriptor.Label, out bool newlyCreated);
            if (variant == null) continue; // save failure — already logged
            if (newlyCreated)
            {
                createdPaths.Add(AssetDatabase.GetAssetPath(variant));
                created++;
            }
            else
            {
                Debug.Log($"[NetworkedPrefabs] Skipping {variant.name} — already exists.");
                skipped++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

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

    // ---------------------------------------------------------------- public API
    // Used by SinglePlayerToMultiplayerConverter to create missing variants
    // on demand during scene conversion. Callers are responsible for running a
    // Coherence bake afterwards when anything reports newlyCreated.

    /// <summary>
    /// Ensures a _Networked sibling exists for the given source prefab asset,
    /// creating it from the matching conversion descriptor if needed. Returns the
    /// networked prefab (existing or new); null when the prefab matches no
    /// descriptor (not a networkable type) or saving failed. Returns the source
    /// itself if it already carries a CoherenceSync.
    /// </summary>
    public static GameObject GetOrCreateNetworkedVariant(GameObject sourceAsset, out bool newlyCreated)
    {
        newlyCreated = false;
        if (sourceAsset == null) return null;
        if (sourceAsset.GetComponent<CoherenceSync>() != null) return sourceAsset;

        foreach (var descriptor in Conversions)
        {
            if (sourceAsset.GetComponent(descriptor.RootComponent) != null)
                return CreateVariant(sourceAsset, descriptor.Bindings, descriptor.PostProcess, descriptor.Label, out newlyCreated);
        }
        return null;
    }

    /// <summary>
    /// Ensures a _Networked sibling exists for a PLAYER prefab, following the
    /// Player_Male_Standard_Gameplay pattern: CoherenceSync syncing Transform
    /// position + rotation and every non-trigger Animator parameter. (The Cmd*
    /// bindings visible on the reference prefab come from [Command] attributes
    /// and are added by coherence automatically.)
    /// </summary>
    public static GameObject GetOrCreateNetworkedPlayerVariant(GameObject playerPrefab, out bool newlyCreated)
    {
        newlyCreated = false;
        if (playerPrefab == null) return null;
        if (playerPrefab.GetComponent<CoherenceSync>() != null) return playerPrefab;

        var bindings = new List<(Type, string)>
        {
            (typeof(Transform), "position"),
            (typeof(Transform), "rotation"),
        };

        var animator = playerPrefab.GetComponent<Animator>();
        var runtimeController = animator != null ? animator.runtimeAnimatorController : null;
        if (runtimeController is AnimatorOverrideController overrideController)
            runtimeController = overrideController.runtimeAnimatorController;

        if (runtimeController is UnityEditor.Animations.AnimatorController controller)
        {
            foreach (var parameter in controller.parameters)
            {
                // Triggers can't be synced as parameters (consumed the frame they fire).
                if (parameter.type != AnimatorControllerParameterType.Trigger)
                    bindings.Add((typeof(Animator), parameter.name));
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkedPrefabs] Player prefab '{playerPrefab.name}' has no resolvable AnimatorController — " +
                             "creating the variant with transform bindings only.");
        }

        return CreateVariant(playerPrefab, bindings.ToArray(), null, "Player", out newlyCreated);
    }

    /// <summary>
    /// Shared creation core: instantiate the source, add CoherenceSync
    /// (NoDuplicates), add the bindings, run the post-process, save as the
    /// _Networked sibling, and register its CoherenceSyncConfig against the
    /// on-disk asset. Returns the existing variant untouched when one is
    /// already present.
    /// </summary>
    private static GameObject CreateVariant(GameObject sourceAsset, (Type Component, string Member)[] bindings,
        Action<GameObject> postProcess, string label, out bool newlyCreated)
    {
        newlyCreated = false;

        string path = AssetDatabase.GetAssetPath(sourceAsset);
        string dir = Path.GetDirectoryName(path).Replace("\\", "/");
        string name = Path.GetFileNameWithoutExtension(path);
        string outputPath = $"{dir}/{name}_Networked.prefab";

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        if (existing != null) return existing;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset);
        try
        {
            var sync = instance.AddComponent<CoherenceSync>();
            sync.uniquenessType = CoherenceSync.UniquenessType.NoDuplicates;

            foreach (var binding in bindings)
            {
                CoherenceSyncUtils.AddBinding(instance, binding.Component, binding.Member);
            }

            postProcess?.Invoke(instance);

            PrefabUtility.SaveAsPrefabAsset(instance, outputPath, out bool success);
            if (!success)
            {
                Debug.LogError($"[NetworkedPrefabs] Failed to save variant: {outputPath}");
                return null;
            }
        }
        finally
        {
            if (instance != null) Object.DestroyImmediate(instance);
        }

        // Register the config against the freshly-loaded asset reference so
        // EditorTarget points at the on-disk prefab (the GameObject returned by
        // SaveAsPrefabAsset can go stale once the scene instance is destroyed,
        // which breaks the subsequent bake).
        var assetOnDisk = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        if (assetOnDisk != null)
            CoherenceSyncConfigUtils.Create(assetOnDisk);

        Debug.Log($"[NetworkedPrefabs] Created {label} variant: {outputPath}");
        newlyCreated = true;
        return assetOnDisk;
    }
}