using System;
using System.Collections.Generic;
using System.IO;
using Coherence.Editor;
using Coherence.Toolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Converts the active scene from single-player to a Coherence multiplayer setup.
// Manual prereqs (done before running):
//   1. Duplicate the playerLevel and position/rotate the duplicate.
//   2. Wire the duplicate into LevelGoal.opponentLevel.
//   3. Wire the duplicate's spawn point into GameManager.opponentSpawnPoint.
// Then run Tools -> Single Player -> Multiplayer Converter -> Convert.
//
// Idempotent: each step skips work that's already done and logs what it did.
public class SinglePlayerToMultiplayerConverter : EditorWindow
{
    private const string LogPrefix = "[SP→MP]";
    private const string LevelGoalPrefabFolder = "Assets/_GAME/02__Prefabs/10_Coherence";
    private const string NetworkedSuffix = "_Networked";

    [MenuItem("Tools/Single Player → Multiplayer Converter")]
    public static void ShowWindow() => GetWindow<SinglePlayerToMultiplayerConverter>("SP→MP Converter");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Convert active scene to multiplayer", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Manual prereqs (do these in the scene first):\n" +
            "  1. Put the whole player level (spawn point, obstacles, tiles, boundaries, collectibles, floor, frame...) under one GameObject.\n" +
            "  2. Duplicate the player Level GameObject; position/rotate the duplicate.\n" +
            "  3. Wire the duplicate into LevelGoal.opponentLevel, and original into LevelGoal.playerLevel.",
            MessageType.Info);
        EditorGUILayout.Space();
        var scene = EditorSceneManager.GetActiveScene();
        EditorGUILayout.LabelField("Active scene:", scene.IsValid() ? scene.name : "(none)");

        EditorGUILayout.Space();
        if (GUILayout.Button("Convert active scene", GUILayout.Height(32)))
            ConvertActiveScene();
    }

    public static void ConvertActiveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError($"{LogPrefix} No active scene to convert.");
            return;
        }

        Debug.Log($"{LogPrefix} ====== Converting scene '{scene.name}' ======");

        var gm = FindInScene<GameManager>(scene);
        if (gm == null) { Debug.LogError($"{LogPrefix} No GameManager in scene. Aborting."); return; }
        var lg = FindInScene<LevelGoal>(scene);
        if (lg == null) { Debug.LogError($"{LogPrefix} No LevelGoal in scene. Aborting."); return; }

        Transform playerLevel, opponentLevel;
        if (!TryReadLevelTransforms(lg, out playerLevel, out opponentLevel))
        {
            Debug.LogError($"{LogPrefix} LevelGoal.playerLevel and LevelGoal.opponentLevel must be set in the scene before running. Aborting.");
            return;
        }

        try
        {
            SetMultiplayerFlag(gm);
            WirePlayerNetworkedPrefab(gm);
            WireSpawnPoint(gm, "playerSpawnPoint", playerLevel, "playerLevel");
            WireSpawnPoint(gm, "opponentSpawnPoint", opponentLevel, "opponentLevel");
            EnsureCoherenceBridge(scene);
            EnsureCoherenceLiveQuery(scene);

            NetworkifyLevelGoal(scene, lg);

            ReplaceNetworkedPrefabInstancesUnder(playerLevel, "playerLevel");
            ReplaceNetworkedPrefabInstancesUnder(opponentLevel, "opponentLevel");

            // playerLevel/opponentLevel transforms may have been invalidated if their root was
            // replaced (the replacement code destroys & reinstantiates). Re-read from LevelGoal.
            if (!TryReadLevelTransforms(lg, out playerLevel, out opponentLevel))
            {
                Debug.LogError($"{LogPrefix} Lost level references after prefab replacement. Aborting.");
                return;
            }

            WireObstacleList(lg, "ObstaclesToDestroy_Player", playerLevel, "playerLevel");
            WireObstacleList(lg, "ObstaclesToDestroy_Opponent", opponentLevel, "opponentLevel");

            SwapLevelGoalSpawnableReferencesInPrefab(scene);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LogPrefix} Conversion failed: {ex}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"{LogPrefix} Running Coherence bake...");
        bool baked = BakeUtil.Bake();
        Debug.Log(baked
            ? $"{LogPrefix} Bake succeeded."
            : $"{LogPrefix} Bake failed — check the Console for Coherence errors.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{LogPrefix} Scene saved. ====== Done ======");
    }

    // ---------- Validation / helpers ----------

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    private static bool TryReadLevelTransforms(LevelGoal lg, out Transform playerLevel, out Transform opponentLevel)
    {
        using var so = new SerializedObject(lg);
        playerLevel = so.FindProperty("playerLevel")?.objectReferenceValue as Transform;
        opponentLevel = so.FindProperty("opponentLevel")?.objectReferenceValue as Transform;
        return playerLevel != null && opponentLevel != null;
    }

    // ---------- Step 1: GameManager.IsMultiplayer ----------

    private static void SetMultiplayerFlag(GameManager gm)
    {
        using var so = new SerializedObject(gm);
        var prop = so.FindProperty("IsMultiplayer");
        if (prop == null)
        {
            Debug.LogWarning($"{LogPrefix} GameManager.IsMultiplayer property not found.");
            return;
        }
        if (prop.boolValue)
        {
            Debug.Log($"{LogPrefix} GameManager.IsMultiplayer already true — skipping.");
            return;
        }
        prop.boolValue = true;
        so.ApplyModifiedProperties();
        Debug.Log($"{LogPrefix} GameManager.IsMultiplayer set to true.");
    }

    // ---------- Step 2: playerNetworkedPrefab ----------

    private static void WirePlayerNetworkedPrefab(GameManager gm)
    {
        using var so = new SerializedObject(gm);
        var networkedProp = so.FindProperty("playerNetworkedPrefab");
        if (networkedProp == null)
        {
            Debug.LogWarning($"{LogPrefix} GameManager.playerNetworkedPrefab field not found.");
            return;
        }
        if (networkedProp.objectReferenceValue != null)
        {
            Debug.Log($"{LogPrefix} GameManager.playerNetworkedPrefab already wired ({networkedProp.objectReferenceValue.name}) — skipping.");
            return;
        }

        var defaultProp = so.FindProperty("playerDefaultPrefab");
        if (defaultProp == null || defaultProp.objectReferenceValue == null)
        {
            Debug.LogWarning($"{LogPrefix} GameManager.playerDefaultPrefab is not set; cannot resolve networked variant.");
            return;
        }

        string defaultPath = AssetDatabase.GetAssetPath(defaultProp.objectReferenceValue);
        string networkedPath = AppendNetworkedSuffix(defaultPath);
        if (string.IsNullOrEmpty(networkedPath))
        {
            Debug.LogWarning($"{LogPrefix} Could not derive networked-prefab path from '{defaultPath}'.");
            return;
        }
        var networkedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(networkedPath);
        if (networkedAsset == null)
        {
            Debug.LogError($"{LogPrefix} Networked player prefab not found at '{networkedPath}'. Create the _Networked sibling and re-run.");
            return;
        }
        var sync = networkedAsset.GetComponent<CoherenceSync>();
        if (sync == null)
        {
            Debug.LogError($"{LogPrefix} '{networkedPath}' has no CoherenceSync on its root.");
            return;
        }
        networkedProp.objectReferenceValue = sync;
        so.ApplyModifiedProperties();
        Debug.Log($"{LogPrefix} Wired playerNetworkedPrefab → '{networkedPath}'.");
    }

    // ---------- Step 3: spawn points ----------

    private static void WireSpawnPoint(GameManager gm, string propName, Transform levelRoot, string levelLabel)
    {
        using var so = new SerializedObject(gm);
        var prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogWarning($"{LogPrefix} GameManager.{propName} not found.");
            return;
        }
        if (prop.objectReferenceValue != null)
        {
            Debug.Log($"{LogPrefix} GameManager.{propName} already wired ({((Transform)prop.objectReferenceValue).name}) — skipping.");
            return;
        }
        var spawn = FindSpawnPoint(levelRoot);
        if (spawn == null)
        {
            Debug.LogError($"{LogPrefix} No GameObject containing 'SpawnPoint' (case-insensitive) found under {levelLabel} ('{levelRoot.name}').");
            return;
        }
        prop.objectReferenceValue = spawn;
        so.ApplyModifiedProperties();
        Debug.Log($"{LogPrefix} Wired GameManager.{propName} → '{spawn.name}' (under {levelLabel}).");
    }

    private static Transform FindSpawnPoint(Transform root)
    {
        var stack = new Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t != root && t.name.IndexOf("SpawnPoint", StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
            for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
        }
        return null;
    }

    // ---------- Step 4: CoherenceBridge / CoherenceLiveQuery ----------

    private static void EnsureCoherenceBridge(Scene scene)
    {
        if (FindInScene<CoherenceBridge>(scene) != null)
        {
            Debug.Log($"{LogPrefix} CoherenceBridge already in scene — skipping.");
            return;
        }
        var go = new GameObject("CoherenceBridge");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<CoherenceBridge>();
        Undo.RegisterCreatedObjectUndo(go, "Create CoherenceBridge");
        Debug.Log($"{LogPrefix} Created CoherenceBridge.");
    }

    private static void EnsureCoherenceLiveQuery(Scene scene)
    {
        if (FindInScene<CoherenceLiveQuery>(scene) != null)
        {
            Debug.Log($"{LogPrefix} CoherenceLiveQuery already in scene — skipping.");
            return;
        }
        var go = new GameObject("CoherenceLiveQuery");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<CoherenceLiveQuery>();
        Undo.RegisterCreatedObjectUndo(go, "Create CoherenceLiveQuery");
        Debug.Log($"{LogPrefix} Created CoherenceLiveQuery.");
    }

    // ---------- Step 5: LevelGoal -> networked prefab ----------

    private static GameObject NetworkifyLevelGoal(Scene scene, LevelGoal lg)
    {
        var go = lg.gameObject;
        EnsureFolder(LevelGoalPrefabFolder);
        string prefabPath = $"{LevelGoalPrefabFolder}/{scene.name}.prefab";

        bool alreadyPrefabConnected =
            PrefabUtility.IsPartOfAnyPrefab(go) &&
            AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(go)) == prefabPath;
        bool hasSync = go.GetComponent<CoherenceSync>() != null;

        if (alreadyPrefabConnected && hasSync)
        {
            Debug.Log($"{LogPrefix} LevelGoal already connected to prefab '{prefabPath}' with CoherenceSync — skipping prefab creation.");
            AssignCoherenceUniqueIds(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        // Edit the scene LevelGoal in place: add CoherenceSync (NoDuplicates) + bindings.
        var sync = go.GetComponent<CoherenceSync>();
        if (sync == null)
        {
            sync = go.AddComponent<CoherenceSync>();
            sync.uniquenessType = CoherenceSync.UniquenessType.NoDuplicates;
            Debug.Log($"{LogPrefix} Added CoherenceSync (NoDuplicates) to scene LevelGoal.");
        }
        else if (sync.uniquenessType != CoherenceSync.UniquenessType.NoDuplicates)
        {
            sync.uniquenessType = CoherenceSync.UniquenessType.NoDuplicates;
            Debug.Log($"{LogPrefix} Updated CoherenceSync.uniquenessType to NoDuplicates on scene LevelGoal.");
        }
        else
        {
            Debug.Log($"{LogPrefix} CoherenceSync already present on scene LevelGoal — keeping.");
        }

        AddBindingIfMissing(go, typeof(Transform), "position");
        AddBindingIfMissing(go, typeof(LevelGoal), nameof(LevelGoal.CmdLoseLevel));
        AddBindingIfMissing(go, typeof(LevelGoal), nameof(LevelGoal.CmdWinLevel));

        // Save (creating or overwriting) and reconnect the scene instance to the prefab.
        bool prefabExisted = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        var savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
        if (savedPrefab == null)
        {
            Debug.LogError($"{LogPrefix} Failed to save LevelGoal as prefab at '{prefabPath}'.");
            return null;
        }
        Debug.Log(prefabExisted
            ? $"{LogPrefix} Updated existing LevelGoal prefab at '{prefabPath}'."
            : $"{LogPrefix} Saved LevelGoal as new prefab → '{prefabPath}'.");

        // Register config so bake picks it up.
        CoherenceSyncConfigUtils.Create(savedPrefab);

        // Ensure the scene instance has scenePrefabInstanceUUID set.
        AssignCoherenceUniqueIds(go);

        return savedPrefab;
    }

    private static void AddBindingIfMissing(GameObject prefabRoot, Type componentType, string member)
    {
        var sync = prefabRoot.GetComponent<CoherenceSync>();
        if (sync == null) return;
        foreach (var b in sync.Bindings)
        {
            if (b == null || b.Descriptor == null) continue;
            if (b.Descriptor.Name == member &&
                b.unityComponent != null &&
                componentType.IsInstanceOfType(b.unityComponent))
            {
                Debug.Log($"{LogPrefix} Binding {componentType.Name}.{member} already present — skipping.");
                return;
            }
        }
        var added = CoherenceSyncUtils.AddBinding(prefabRoot, componentType, member);
        if (added != null) Debug.Log($"{LogPrefix} Added binding {componentType.Name}.{member}.");
        else Debug.LogWarning($"{LogPrefix} Failed to add binding {componentType.Name}.{member} — descriptor not found.");
    }

    // ---------- Step 6: replace prefab instances with _Networked siblings ----------

    private static void ReplaceNetworkedPrefabInstancesUnder(Transform root, string label)
    {
        if (root == null) return;

        var instances = new List<(GameObject instance, GameObject networkedPrefab, string sourcePath)>();
        // Start at the level root's children so the level container itself is never replaced.
        for (int i = 0; i < root.childCount; i++)
            CollectReplaceableInstances(root.GetChild(i), instances);

        if (instances.Count == 0)
        {
            Debug.Log($"{LogPrefix} {label}: no prefab instances need replacing.");
            return;
        }

        Debug.Log($"{LogPrefix} {label}: replacing {instances.Count} prefab instances with _Networked siblings.");

        int replaced = 0;
        foreach (var (instance, networkedPrefab, sourcePath) in instances)
        {
            if (instance == null) continue;
            try
            {
                var newInstance = ReplaceWithPrefab(instance, networkedPrefab);
                AssignCoherenceUniqueIds(newInstance);
                replaced++;
                Debug.Log($"{LogPrefix}   replaced '{newInstance.name}' (source: {Path.GetFileName(sourcePath)} → {Path.GetFileNameWithoutExtension(sourcePath)}{NetworkedSuffix}).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Failed to replace '{instance?.name}': {ex.Message}");
            }
        }
        Debug.Log($"{LogPrefix} {label}: replaced {replaced} / {instances.Count}.");
    }

    private static void CollectReplaceableInstances(Transform t, List<(GameObject, GameObject, string)> result)
    {
        // Snapshot children before recursing — entries may be replaced under us.
        var children = new List<Transform>();
        for (int i = 0; i < t.childCount; i++) children.Add(t.GetChild(i));

        var go = t.gameObject;

        if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
        {
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
            if (source != null)
            {
                string sourcePath = AssetDatabase.GetAssetPath(source);
                string sourceName = Path.GetFileNameWithoutExtension(sourcePath);

                // Already a networked variant — skip.
                if (sourceName.EndsWith(NetworkedSuffix, StringComparison.Ordinal))
                {
                    // Still ensure scenePrefabInstanceUUID is set on already-networked instances.
                    AssignCoherenceUniqueIds(go);
                }
                else
                {
                    string networkedPath = AppendNetworkedSuffix(sourcePath);
                    var networkedAsset = !string.IsNullOrEmpty(networkedPath)
                        ? AssetDatabase.LoadAssetAtPath<GameObject>(networkedPath)
                        : null;
                    if (networkedAsset != null)
                    {
                        result.Add((go, networkedAsset, sourcePath));
                        return; // don't descend into a node we're about to replace
                    }
                }
            }
        }

        foreach (var c in children)
            if (c != null) CollectReplaceableInstances(c, result);
    }

    private static GameObject ReplaceWithPrefab(GameObject instance, GameObject networkedPrefab)
    {
        var parent = instance.transform.parent;
        int siblingIndex = instance.transform.GetSiblingIndex();
        var localPos = instance.transform.localPosition;
        var localRot = instance.transform.localRotation;
        var localScale = instance.transform.localScale;
        bool activeSelf = instance.activeSelf;
        int layer = instance.layer;
        string tag = instance.tag;
        var staticFlags = GameObjectUtility.GetStaticEditorFlags(instance);
        var scene = instance.scene;

        Object.DestroyImmediate(instance);

        var newInstance = (GameObject)PrefabUtility.InstantiatePrefab(networkedPrefab, scene);
        if (parent != null) newInstance.transform.SetParent(parent, false);
        newInstance.transform.localPosition = localPos;
        newInstance.transform.localRotation = localRot;
        newInstance.transform.localScale = localScale;
        newInstance.transform.SetSiblingIndex(siblingIndex);
        newInstance.SetActive(activeSelf);
        newInstance.layer = layer;
        try { newInstance.tag = tag; } catch { }
        GameObjectUtility.SetStaticEditorFlags(newInstance, staticFlags);

        return newInstance;
    }

    private static void AssignCoherenceUniqueIds(GameObject go)
    {
        if (go == null) return;
        foreach (var sync in go.GetComponentsInChildren<CoherenceSync>(true))
        {
            using var so = new SerializedObject(sync);
            var uuidProp = so.FindProperty("scenePrefabInstanceUUID");
            if (uuidProp == null || !string.IsNullOrEmpty(uuidProp.stringValue))
                continue;

            // Mirror what the inspector refresh button does: use m_FileID (stable scene-file identifier).
            var fileIdProp = so.FindProperty("m_GameObject.m_FileID");
            int id = (fileIdProp != null && fileIdProp.intValue != 0)
                ? fileIdProp.intValue
                : sync.GetInstanceID();

            uuidProp.stringValue = id.ToString();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ---------- Step 7: wire LevelGoal obstacle lists ----------

    private static void WireObstacleList(LevelGoal lg, string listProp, Transform levelRoot, string label)
    {
        if (levelRoot == null) return;

        var underLevel = levelRoot.GetComponentsInChildren<Obstacle>(true);

        using var so = new SerializedObject(lg);
        var list = so.FindProperty(listProp);
        if (list == null || !list.isArray)
        {
            Debug.LogWarning($"{LogPrefix} LevelGoal.{listProp} not found or not an array.");
            return;
        }

        // First pass: prune null/dangling entries left over from destroyed singleplayer obstacles.
        int pruned = 0;
        for (int i = list.arraySize - 1; i >= 0; i--)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                list.DeleteArrayElementAtIndex(i);
                pruned++;
            }
        }
        if (pruned > 0)
            Debug.Log($"{LogPrefix} LevelGoal.{listProp}: pruned {pruned} null entries (dangling after replacement).");

        var existing = new HashSet<Obstacle>();
        for (int i = 0; i < list.arraySize; i++)
        {
            var entry = list.GetArrayElementAtIndex(i).objectReferenceValue as Obstacle;
            if (entry != null) existing.Add(entry);
        }

        int added = 0;
        int skippedInactive = 0;
        foreach (var obs in underLevel)
        {
            if (obs == null) continue;
            // Inactive obstacles are part of the fixed-falling spawn system — they get activated at
            // runtime and the spawn code adds them to the destroy list then. Skip here.
            if (!obs.gameObject.activeInHierarchy)
            {
                skippedInactive++;
                continue;
            }
            if (existing.Contains(obs)) continue;
            int idx = list.arraySize;
            list.InsertArrayElementAtIndex(idx);
            list.GetArrayElementAtIndex(idx).objectReferenceValue = obs;
            existing.Add(obs);
            added++;
        }
        so.ApplyModifiedProperties();

        string inactiveNote = skippedInactive > 0 ? $", skipped {skippedInactive} inactive (fixed-falling pool)" : "";
        if (added == 0)
            Debug.Log($"{LogPrefix} LevelGoal.{listProp}: all {existing.Count} active obstacles under {label} already wired{inactiveNote}.");
        else
            Debug.Log($"{LogPrefix} LevelGoal.{listProp}: added {added} obstacles from {label} (total now {existing.Count}){inactiveNote}.");
    }

    // ---------- Step 8: swap LevelGoal spawnable refs to networked variants on the prefab ----------

    // The LevelGoal's spawnable lists hold prefab-asset references that get instantiated at runtime.
    // We modify the prefab asset directly (LoadPrefabContents) so the swap becomes the prefab default,
    // rather than instance overrides on the scene LevelGoal.
    private static void SwapLevelGoalSpawnableReferencesInPrefab(Scene scene)
    {
        string prefabPath = $"{LevelGoalPrefabFolder}/{scene.name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            Debug.LogWarning($"{LogPrefix} LevelGoal prefab not found at '{prefabPath}'; skipping spawnable-list swap.");
            return;
        }

        var contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var prefabLg = contents.GetComponent<LevelGoal>();
            if (prefabLg == null)
            {
                Debug.LogError($"{LogPrefix} No LevelGoal component on prefab '{prefabPath}'.");
                return;
            }

            var so = new SerializedObject(prefabLg);
            int swapped = 0;

            swapped += SwapArrayItems(so, "FallingObstacles", typeof(Obstacle));
            swapped += SwapArrayItems(so, "FallingBombs", typeof(GameObject));
            swapped += SwapArrayItems(so, "FallingCollectibles", typeof(GameObject));
            swapped += SwapArrayItems(so, "fixedFallingObstacles", typeof(Obstacle));
            swapped += SwapArrayItems(so, "fixedFallingBombs", typeof(GameObject));
            swapped += SwapArrayItems(so, "fixedFallingCollectibles", typeof(GameObject));
            if (SwapSingleObjectProp(so, "bombUniversalPrefab", typeof(GameObject))) swapped++;

            if (swapped > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath, out bool success);
                Debug.Log(success
                    ? $"{LogPrefix} LevelGoal prefab: swapped {swapped} spawnable references → _Networked."
                    : $"{LogPrefix} LevelGoal prefab: swap done in-memory but SaveAsPrefabAsset failed.");
            }
            else
            {
                Debug.Log($"{LogPrefix} LevelGoal prefab: spawnable references already networked (or no _Networked siblings) — no changes.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static int SwapArrayItems(SerializedObject so, string arrayPropName, Type expectedItemType)
    {
        var arr = so.FindProperty(arrayPropName);
        if (arr == null || !arr.isArray)
        {
            Debug.LogWarning($"{LogPrefix} LevelGoal.{arrayPropName} not found or not an array.");
            return 0;
        }

        int swapped = 0;
        for (int i = 0; i < arr.arraySize; i++)
        {
            var elem = arr.GetArrayElementAtIndex(i);
            var itemProp = elem.FindPropertyRelative("item");
            if (itemProp == null) continue;
            var current = itemProp.objectReferenceValue;
            if (current == null) continue;

            var replacement = ResolveNetworkedReplacement(current, expectedItemType, out string reason);
            if (replacement == null)
            {
                if (reason != null) Debug.Log($"{LogPrefix}   {arrayPropName}[{i}]: {reason}");
                continue;
            }
            if (replacement == current) continue;

            string oldName = current.name;
            itemProp.objectReferenceValue = replacement;
            swapped++;
            Debug.Log($"{LogPrefix}   {arrayPropName}[{i}]: {oldName} → {replacement.name}");
        }
        return swapped;
    }

    private static bool SwapSingleObjectProp(SerializedObject so, string propName, Type expectedType)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) return false;
        var current = prop.objectReferenceValue;
        if (current == null) return false;

        var replacement = ResolveNetworkedReplacement(current, expectedType, out string reason);
        if (replacement == null)
        {
            if (reason != null) Debug.Log($"{LogPrefix}   {propName}: {reason}");
            return false;
        }
        if (replacement == current) return false;

        string oldName = current.name;
        prop.objectReferenceValue = replacement;
        Debug.Log($"{LogPrefix}   {propName}: {oldName} → {replacement.name}");
        return true;
    }

    // Returns the networked replacement Object, or null if no swap is possible/needed.
    // 'reason' is set to a short human-readable string when null is returned for a non-trivial reason
    // (already networked, missing sibling, etc.) so callers can log.
    private static Object ResolveNetworkedReplacement(Object current, Type expectedType, out string reason)
    {
        reason = null;
        string currentPath = AssetDatabase.GetAssetPath(current);
        if (string.IsNullOrEmpty(currentPath))
        {
            reason = $"'{current.name}' is not an asset reference.";
            return null;
        }

        string name = Path.GetFileNameWithoutExtension(currentPath);
        if (name.EndsWith(NetworkedSuffix, StringComparison.Ordinal))
            return null; // already networked, silent skip

        string networkedPath = AppendNetworkedSuffix(currentPath);
        if (string.IsNullOrEmpty(networkedPath)) return null;

        var networkedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(networkedPath);
        if (networkedAsset == null)
        {
            reason = $"no networked sibling at '{networkedPath}'.";
            return null;
        }

        if (expectedType == typeof(GameObject))
            return networkedAsset;

        var component = networkedAsset.GetComponent(expectedType);
        if (component == null)
        {
            reason = $"'{networkedPath}' has no {expectedType.Name} component.";
            return null;
        }
        return component;
    }

    // ---------- utilities ----------

    private static string AppendNetworkedSuffix(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;
        string dir = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        string name = Path.GetFileNameWithoutExtension(assetPath);
        string ext = Path.GetExtension(assetPath);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name)) return null;
        return $"{dir}/{name}{NetworkedSuffix}{ext}";
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        var parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
                Debug.Log($"{LogPrefix} Created folder '{next}'.");
            }
            current = next;
        }
    }
}
