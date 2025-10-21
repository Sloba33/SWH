#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class PrefabColorRenamer : EditorWindow
{
    public List<GameObject> prefabs = new List<GameObject>();

    [MenuItem("Tools/Prefab Color Renamer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabColorRenamer>("Prefab Color Renamer");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);
        SerializedProperty sp = so.FindProperty("prefabs");

        EditorGUILayout.PropertyField(sp, true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("🧹 Rename Prefabs (Preserve _Unmatched)"))
        {
            RenamePrefabs();
        }
    }

    private void RenamePrefabs()
    {
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null) continue;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
                continue;

            string originalName = Path.GetFileNameWithoutExtension(prefabPath);
            bool hadUnmatched = originalName.EndsWith("_Unmatched", StringComparison.OrdinalIgnoreCase);

            // Remove "_Unmatched" before processing
            string baseName = originalName;
            if (hadUnmatched)
                baseName = baseName.Substring(0, baseName.Length - "_Unmatched".Length);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                List<string> colors = new List<string>();

                foreach (Transform child in prefabRoot.transform)
                {
                    if (child.name.IndexOf("outline", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    string raw = child.name.Trim();
                    raw = Regex.Replace(raw, @"\s*\(.*?\)\s*$", "");
                    raw = raw.Replace("(Clone)", "").Trim();

                    if (string.IsNullOrEmpty(raw)) continue;

                    string normalized = raw.Length > 1
                        ? char.ToUpper(raw[0]) + raw.Substring(1).ToLower()
                        : raw.ToUpper();

                    colors.Add(normalized);
                }

                if (colors.Count == 0)
                {
                    Debug.LogWarning($"[{originalName}] No colors found, skipping rename.");
                    continue;
                }

                // Deduplicate + sort alphabetically
                colors = colors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                colors.Sort(StringComparer.OrdinalIgnoreCase);

                string colorSuffix = string.Join("", colors);

                // --- build new name ---
                string newName = baseName;

                // Remove existing color suffix (if present)
                int lastUnderscore = newName.LastIndexOf('_');
                if (lastUnderscore >= 0)
                {
                    // Remove possible old color suffix like "_BlueRedYellow"
                    string possibleSuffix = newName.Substring(lastUnderscore + 1);
                    if (Regex.IsMatch(possibleSuffix, @"^[A-Z][a-zA-Z]+$"))
                        newName = newName.Substring(0, lastUnderscore);
                }

                newName += "_" + colorSuffix;

                // Re-append _Unmatched if it was there originally
                if (hadUnmatched)
                    newName += "_Unmatched";

                if (newName == originalName)
                {
                    Debug.Log($"[{originalName}] Already correct, skipping.");
                    continue;
                }

                string err = AssetDatabase.RenameAsset(prefabPath, newName);
                if (!string.IsNullOrEmpty(err))
                    Debug.LogError($"[{originalName}] Rename error: {err}");
                else
                    Debug.Log($"[{originalName}] ✅ Renamed → {newName}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
