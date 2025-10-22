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

        EditorGUILayout.Space(10);
        // --- New fuzzy color match button ---
        if (GUILayout.Button("🎯 Find Closest Color Match (Logs Only)"))
        {
            FindClosestColorMatch();
        }
    }

    // ------------------------------------------------------
    // 🎯 NEW FEATURE: Find best color-overlap prefab
    // ------------------------------------------------------
    private void FindClosestColorMatch()
    {
        if (scenes.Count == 0 || prefabs.Count == 0)
        {
            Debug.LogError("[Matcher-Fuzzy] Please assign at least one scene and some prefabs.");
            return;
        }

        SceneAsset sceneAsset = scenes[0];
        if (sceneAsset == null)
        {
            Debug.LogError("[Matcher-Fuzzy] First scene in list is null.");
            return;
        }

        string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        List<string> sceneColors = ExtractColorList(sceneName);

        if (sceneColors.Count == 0)
        {
            Debug.Log($"[Matcher-Fuzzy] Scene {sceneName} has no color info.");
            return;
        }

        int colorCount = sceneColors.Count;

        // Build list of all eligible prefabs with overlap scores
        var scoredPrefabs = new List<(string prefabName, int overlap, List<string> prefabColors)>();

        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;
            string prefabName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));

            List<string> prefabColors = ExtractColorList(prefabName);
            if (prefabColors.Count != colorCount)
                continue; // must have same number of colors

            int overlap = prefabColors.Intersect(sceneColors, StringComparer.OrdinalIgnoreCase).Count();
            if (overlap > 0)
                scoredPrefabs.Add((prefabName, overlap, prefabColors));
        }

        if (scoredPrefabs.Count == 0)
        {
            Debug.Log($"[Matcher-Fuzzy] ❌ No prefab with {colorCount} colors found for {sceneName}");
            return;
        }

        // Sort by overlap descending, then alphabetically for stability
        var topMatches = scoredPrefabs
            .OrderByDescending(p => p.overlap)
            .ThenBy(p => p.prefabName, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        Debug.Log($"[Matcher-Fuzzy] 🎯 Top matches for scene '{sceneName}' " +
                  $"({colorCount} colors: {string.Join(", ", sceneColors)}):");

        int rank = 1;
        foreach (var match in topMatches)
        {
            var sceneOnly = sceneColors.Except(match.prefabColors, StringComparer.OrdinalIgnoreCase).ToList();
            var prefabOnly = match.prefabColors.Except(sceneColors, StringComparer.OrdinalIgnoreCase).ToList();

            Debug.Log($"   {rank}. Prefab: {match.prefabName}  " +
                      $"→ {match.overlap}/{colorCount} matching colors\n" +
                      $"      Colors: {string.Join(", ", match.prefabColors)}");

            // Add replacement suggestion
            if (sceneOnly.Count == prefabOnly.Count && sceneOnly.Count > 0)
            {
                for (int i = 0; i < sceneOnly.Count; i++)
                    Debug.Log($"      💡 Suggestion: Replace '{sceneOnly[i]}' in scene with '{prefabOnly[i]}'");
            }
            else if (sceneOnly.Count > 0 || prefabOnly.Count > 0)
            {
                Debug.Log($"      💡 Suggestion: Adjust colors → Scene has [{string.Join(", ", sceneOnly)}], " +
                          $"Prefab has [{string.Join(", ", prefabOnly)}]");
            }

            rank++;
        }
    }

    // Helper for fuzzy match
    private List<string> ExtractColorList(string name)
    {
        name = name.Replace("_Matched", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("_Unmatched", "", StringComparison.OrdinalIgnoreCase);

        int underscoreIndex = name.LastIndexOf('_');
        if (underscoreIndex == -1 || underscoreIndex == name.Length - 1)
            return new List<string>();

        string suffix = name.Substring(underscoreIndex + 1);
        var colorMatches = Regex.Matches(suffix, @"[A-Z][a-z]*");
        var colors = colorMatches.Cast<Match>().Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        colors.Sort(StringComparer.OrdinalIgnoreCase);
        return colors;
    }

    // ------------------------------------------------------
    // EXISTING MATCHING SYSTEM BELOW (unchanged)
    // ------------------------------------------------------
    private void MatchScenesToPrefabs()
    {
        if (scenes.Count == 0 || prefabs.Count == 0)
        {
            Debug.LogError("[Matcher] Please assign both scenes and prefabs before running.");
            return;
        }

        string activeScenePath = SceneManager.GetActiveScene().path;

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
                    string sceneBase = sceneName;
                    int colorIndex = sceneBase.IndexOf('_');
                    if (colorIndex > 0)
                        sceneBase = sceneBase.Substring(0, colorIndex);

                    sceneBase = sceneBase.Replace("/", "-").Replace("\\", "-").Trim();
                    sceneBase = Regex.Replace(sceneBase, @"[^a-zA-Z0-9_\\-\\s]", "");

                    RenameWithSuffix(scenePath, "_Matched");
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
