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
        var prefabsByKey = new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase);
        var matchedPrefabs = new HashSet<GameObject>();

        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;
            string path = AssetDatabase.GetAssetPath(prefab);
            string name = Path.GetFileNameWithoutExtension(path);
            string key = ExtractColorKey(name);

            if (!string.IsNullOrEmpty(key) && !name.EndsWith("_Matched", StringComparison.OrdinalIgnoreCase))
            {
                if (!prefabsByKey.ContainsKey(key))
                    prefabsByKey[key] = new List<GameObject>();
                prefabsByKey[key].Add(prefab);
            }
        }

        // Tracking
        List<string> matchedScenes = new();
        List<string> unmatchedScenes = new();
        List<string> ignoredScenes = new();
        List<string> ignoredPrefabs = new();

        int total = 0, matched = 0, unmatched = 0;

        foreach (var sceneAsset in scenes)
        {
            if (sceneAsset == null) continue;
            total++;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            // Skip already matched scenes
            if (sceneName.EndsWith("_Matched", StringComparison.OrdinalIgnoreCase))
            {
                ignoredScenes.Add(sceneName);
                continue;
            }

            string sceneKey = ExtractColorKey(sceneName);
            if (string.IsNullOrEmpty(sceneKey))
            {
                Debug.Log($"[Matcher] No color key found for {sceneName}");
                unmatched++;
                unmatchedScenes.Add(sceneName);
                continue;
            }

            if (prefabsByKey.TryGetValue(sceneKey, out List<GameObject> availablePrefabs) && availablePrefabs.Count > 0)
            {
                GameObject prefabObj = availablePrefabs[0];
                availablePrefabs.RemoveAt(0);

                string prefabPath = AssetDatabase.GetAssetPath(prefabObj);
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                Debug.Log($"[Matcher] ✅ Match found for {sceneName} with Prefab {prefabName}");
                matched++;
                matchedScenes.Add($"{sceneName} → {prefabName}");
                matchedPrefabs.Add(prefabObj);

                // Assign prefab BEFORE renaming the scene
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
                    // Extract scene base name (everything before color suffix)
                    string sceneBase = sceneName;
                    int colorIndex = sceneBase.IndexOf('_');
                    if (colorIndex > 0)
                        sceneBase = sceneBase.Substring(0, colorIndex);

                    // Clean up scene base for safe naming
                    sceneBase = sceneBase.Replace("/", "-").Replace("\\", "-").Trim();
                    sceneBase = Regex.Replace(sceneBase, @"[^a-zA-Z0-9_\-\s]", ""); // remove weird chars

                    // Rename scene first
                    RenameWithSuffix(scenePath, "_Matched");

                    // Rename prefab with scene name included
                    RenameWithSuffix(prefabPath, $"_Matched_{sceneBase}");
                }
            }
            else
            {
                Debug.Log($"[Matcher] ❌ No match found for {sceneName}");
                unmatched++;
                unmatchedScenes.Add(sceneName);
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
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

                if (prefabName.EndsWith("_Matched", StringComparison.OrdinalIgnoreCase))
                {
                    ignoredPrefabs.Add(prefabName);
                    continue;
                }

                if (!matchedPrefabs.Contains(prefab))
                {
                    RenameWithSuffix(prefabPath, "_Unmatched");
                }
            }
        }

        // --- Step 3: Summary ---
        Debug.Log($"[Matcher] ✅ Done! Processed {total} scenes. Matched: {matched}, Unmatched: {unmatched}, Ignored (already matched): {ignoredScenes.Count}");
        Debug.Log("──────────────────────────────");

        if (matchedScenes.Count > 0)
            Debug.Log("[Matcher] ✅ Matched Scenes:\n" + string.Join("\n", matchedScenes));

        if (unmatchedScenes.Count > 0)
            Debug.Log("[Matcher] ❌ Unmatched Scenes:\n" + string.Join("\n", unmatchedScenes));

        if (ignoredScenes.Count > 0)
            Debug.Log("[Matcher] 🚫 Ignored Scenes (already _Matched):\n" + string.Join("\n", ignoredScenes));

        if (ignoredPrefabs.Count > 0)
            Debug.Log("[Matcher] 🚫 Ignored Prefabs (already _Matched):\n" + string.Join("\n", ignoredPrefabs));

        Debug.Log("──────────────────────────────");
    }

    private string ExtractColorKey(string name)
    {
        // Remove _Matched/_Unmatched for cleaner comparisons
        name = name.Replace("_Matched", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("_Unmatched", "", StringComparison.OrdinalIgnoreCase);

        int underscoreIndex = name.LastIndexOf('_');
        if (underscoreIndex == -1 || underscoreIndex == name.Length - 1)
            return string.Empty;

        string suffix = name.Substring(underscoreIndex + 1);
        suffix = new string(suffix.Where(char.IsLetter).ToArray());
        return suffix;
    }

    private void RenameWithSuffix(string assetPath, string suffix)
    {
        string name = Path.GetFileNameWithoutExtension(assetPath);

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
