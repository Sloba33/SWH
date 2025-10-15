#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePrefabMatcher : EditorWindow
{
    public List<SceneAsset> scenes = new List<SceneAsset>();
    public List<GameObject> prefabs = new List<GameObject>();
    public bool performMatch = false;
    public bool assignToLevelGoal = false;

    [MenuItem("Tools/Scene–Prefab Matcher")]
    public static void ShowWindow()
    {
        GetWindow<ScenePrefabMatcher>("Scene–Prefab Matcher");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);
        EditorGUILayout.LabelField("Scene–Prefab Matcher", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(so.FindProperty("scenes"), new GUIContent("Scenes to Match"), true);
        EditorGUILayout.PropertyField(so.FindProperty("prefabs"), new GUIContent("Prefabs to Match"), true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        performMatch = EditorGUILayout.Toggle("Perform Matching (rename assets)", performMatch);
        assignToLevelGoal = EditorGUILayout.Toggle("Assign Prefab in Scene to LevelGoal", assignToLevelGoal);

        EditorGUILayout.Space();
        if (GUILayout.Button(performMatch ? "⚙️ Run Matching" : "🔍 Test Matching (Logs Only)"))
        {
            MatchScenesToPrefabs();
        }
    }

    private void MatchScenesToPrefabs()
    {
        if (scenes.Count == 0 || prefabs.Count == 0)
        {
            Debug.LogError("[Matcher] Please assign both scenes and prefabs before running.");
            return;
        }

        string activeScenePath = SceneManager.GetActiveScene().path;

        // --- Step 1: Prepare prefab dictionary ---
        // Group prefabs by color key (multiple prefabs can have same key)
        var prefabsByKey = new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase);
        var matchedPrefabs = new HashSet<GameObject>();

        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;
            string path = AssetDatabase.GetAssetPath(prefab);
            string name = Path.GetFileNameWithoutExtension(path);
            string key = ExtractColorKey(name);
            if (!string.IsNullOrEmpty(key))
            {
                if (!prefabsByKey.ContainsKey(key))
                    prefabsByKey[key] = new List<GameObject>();
                prefabsByKey[key].Add(prefab);
            }
        }

        int total = 0, matched = 0, unmatched = 0;

        foreach (var sceneAsset in scenes)
        {
            if (sceneAsset == null) continue;
            total++;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            string sceneKey = ExtractColorKey(sceneName);

            if (string.IsNullOrEmpty(sceneKey))
            {
                Debug.Log($"[Matcher] No color key found for {sceneName}");
                unmatched++;
                continue;
            }

            if (prefabsByKey.TryGetValue(sceneKey, out List<GameObject> availablePrefabs) && availablePrefabs.Count > 0)
            {
                // Take the first available prefab for this color key
                GameObject prefabObj = availablePrefabs[0];
                availablePrefabs.RemoveAt(0); // Remove it so it won't be reused
                
                string prefabPath = AssetDatabase.GetAssetPath(prefabObj);
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                Debug.Log($"[Matcher] ✅ Match found for {sceneName} with Prefab {prefabName}");
                matched++;
                matchedPrefabs.Add(prefabObj);

                if (assignToLevelGoal && performMatch)
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    LevelGoal goal = GameObject.FindObjectOfType<LevelGoal>();
                    if (goal != null)
                    {
                        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        LevelProgress progress = prefabAsset?.GetComponent<LevelProgress>();
                        if (progress != null)
                        {
                            goal.levelProgress = progress;
                            EditorUtility.SetDirty(goal);
                            EditorSceneManager.MarkSceneDirty(scene);
                            EditorSceneManager.SaveScene(scene);
                            Debug.Log($"[Matcher] Assigned {prefabName} to LevelGoal in {sceneName}");
                        }
                    }
                }

                if (performMatch)
                {
                    RenameWithSuffix(scenePath, "_Matched");
                    RenameWithSuffix(prefabPath, "_Matched");
                }
            }
            else
            {
                Debug.Log($"[Matcher] ❌ No match found for {sceneName}");
                unmatched++;
                if (performMatch)
                    RenameWithSuffix(scenePath, "_Unmatched");
            }
        }

        // --- Step 2: Handle unused prefabs ---
        if (performMatch)
        {
            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;
                
                // Check by object reference, not path (path changes after renaming!)
                if (!matchedPrefabs.Contains(prefab))
                {
                    string prefabPath = AssetDatabase.GetAssetPath(prefab);
                    // If it was never matched, mark it as unmatched
                    RenameWithSuffix(prefabPath, "_Unmatched");
                }
            }
        }

        // --- Step 3: Restore previous scene ---
        // if (!string.IsNullOrEmpty(activeScenePath))
        //     EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

        Debug.Log($"[Matcher] ✅ Done! Processed: {total}, Matched: {matched}, Unmatched: {unmatched}");
    }

    private string ExtractColorKey(string name)
    {
        int underscoreIndex = name.LastIndexOf('_');
        if (underscoreIndex == -1 || underscoreIndex == name.Length - 1)
            return string.Empty;

        string suffix = name.Substring(underscoreIndex + 1);
        suffix = suffix.Replace("_Matched", "").Replace("_Unmatched", "");
        suffix = new string(suffix.Where(char.IsLetter).ToArray());
        return suffix;
    }

    private void RenameWithSuffix(string assetPath, string suffix)
    {
        string name = Path.GetFileNameWithoutExtension(assetPath);

        // Replace suffixes if needed
        if (suffix == "_Matched" && name.EndsWith("_Unmatched", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(0, name.Length - "_Unmatched".Length);
        else if (suffix == "_Unmatched" && name.EndsWith("_Matched", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(0, name.Length - "_Matched".Length);

        if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            string newName = name + suffix;
            string err = AssetDatabase.RenameAsset(assetPath, newName);
            if (!string.IsNullOrEmpty(err))
                Debug.LogError($"[Matcher] Rename error for {name}: {err}");
            else
                Debug.Log($"[Matcher] Renamed {name} → {newName}");
        }
    }
}
#endif