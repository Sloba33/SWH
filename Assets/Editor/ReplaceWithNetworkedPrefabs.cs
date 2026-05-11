using System.Collections.Generic;
using Coherence.Toolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReplaceWithNetworkedPrefabs
{
    [MenuItem("Tools/Replace Obstacles with Networked Prefabs")]
    public static void Execute()
    {
        string folder = "Assets/_GAME/02__Prefabs/02_Obstacles/___FOR USE___/SP/Plastic_Cross";

        // Map color -> networked prefab
        var colors = new[] { "Blue", "Green", "Red", "Yellow", "Purple" };
        var networkedByColor = new Dictionary<string, GameObject>();
        foreach (var c in colors)
        {
            string p = $"{folder}/Plastic_Cross_{c}_Networked.prefab";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null)
            {
                Debug.LogError($"Networked prefab not found at {p}");
                return;
            }
            networkedByColor[c] = go;
        }

        // Map source prefab GUID -> target networked prefab
        var sourceToNetworked = new Dictionary<string, GameObject>();
        foreach (var c in colors)
        {
            string[] sourceNames = { $"Plastic_Cross_{c}", $"Plastic_Cross_{c} 1" };
            foreach (var sn in sourceNames)
            {
                string path = $"{folder}/{sn}.prefab";
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"Source prefab not found: {path}");
                    continue;
                }
                sourceToNetworked[guid] = networkedByColor[c];
            }
        }

        var scene = EditorSceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();

        var toReplace = new List<(GameObject instance, GameObject networkedPrefab)>();

        foreach (var root in rootObjects)
        {
            CollectInstances(root.transform, sourceToNetworked, toReplace);
        }

        Debug.Log($"Found {toReplace.Count} instances to replace.");

        int replacedCount = 0;
        foreach (var (instance, networkedPrefab) in toReplace)
        {
            var parent = instance.transform.parent;
            int siblingIndex = instance.transform.GetSiblingIndex();
            var scale = instance.transform.localScale;
            var localPos = instance.transform.localPosition;
            var localRot = instance.transform.localRotation;
            string oldName = instance.name;
            bool activeSelf = instance.activeSelf;
            int layer = instance.layer;
            string tag = instance.tag;
            var staticFlags = GameObjectUtility.GetStaticEditorFlags(instance);

            Object.DestroyImmediate(instance);

            var newInstance = (GameObject)PrefabUtility.InstantiatePrefab(networkedPrefab, scene);
            if (parent != null) newInstance.transform.SetParent(parent, false);
            newInstance.transform.localPosition = localPos;
            newInstance.transform.localRotation = localRot;
            newInstance.transform.localScale = scale;
            newInstance.transform.SetSiblingIndex(siblingIndex);
            newInstance.SetActive(activeSelf);
            newInstance.layer = layer;
            try { newInstance.tag = tag; } catch { }
            GameObjectUtility.SetStaticEditorFlags(newInstance, staticFlags);
            AssignCoherenceUniqueIds(newInstance);

            replacedCount++;
        }

        Debug.Log($"Replaced {replacedCount} obstacle instances with networked versions.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AssignCoherenceUniqueIds(GameObject go)
    {
        foreach (var sync in go.GetComponentsInChildren<CoherenceSync>(true))
        {
            using var so = new SerializedObject(sync);
            var uuidProp = so.FindProperty("scenePrefabInstanceUUID");
            if (uuidProp == null || !string.IsNullOrEmpty(uuidProp.stringValue))
                continue;

            // Mirror what the inspector refresh button does: use m_FileID (stable scene-file identifier)
            var fileIdProp = so.FindProperty("m_GameObject.m_FileID");
            int id = (fileIdProp != null && fileIdProp.intValue != 0)
                ? fileIdProp.intValue
                : sync.GetInstanceID();

            uuidProp.stringValue = id.ToString();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CollectInstances(Transform t, Dictionary<string, GameObject> sourceToNetworked, List<(GameObject, GameObject)> list)
    {
        var children = new List<Transform>();
        for (int i = 0; i < t.childCount; i++) children.Add(t.GetChild(i));

        var go = t.gameObject;

        if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
        {
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
            if (source != null)
            {
                string path = AssetDatabase.GetAssetPath(source);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid) && sourceToNetworked.TryGetValue(guid, out var networked))
                {
                    list.Add((go, networked));
                    return;
                }
            }
        }

        foreach (var child in children)
        {
            if (child != null) CollectInstances(child, sourceToNetworked, list);
        }
    }
}
