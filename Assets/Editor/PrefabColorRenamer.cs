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
        if (GUILayout.Button("Rename Prefabs"))
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

            string baseName = Path.GetFileNameWithoutExtension(prefabPath);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // Debug: list children
                var children = prefabRoot.transform.Cast<Transform>().Select(t => t.name).ToArray();
                Debug.Log($"[{baseName}] prefab root children: {string.Join(", ", children)}");

                List<string> colors = new List<string>();
                foreach (Transform child in prefabRoot.transform)
                {
                    // Skip outlines
                    if (child.name.IndexOf("outline", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    string raw = child.name.Trim();
                    raw = Regex.Replace(raw, @"\s*\(.*?\)\s*$", ""); // strip (1), (Clone)
                    raw = raw.Replace("(Clone)", "").Trim();

                    if (string.IsNullOrEmpty(raw)) continue;

                    // Normalize
                    string normalized = raw.Length > 1
                        ? char.ToUpper(raw[0]) + raw.Substring(1).ToLower()
                        : raw.ToUpper();

                    colors.Add(normalized);
                }

                if (colors.Count == 0)
                {
                    Debug.LogWarning($"[{baseName}] No colors found, skipping rename.");
                    continue;
                }

                // Deduplicate + sort
                colors = colors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                colors.Sort(StringComparer.OrdinalIgnoreCase);

                string colorSuffix = string.Join("", colors);
                string newName = baseName + "_" + colorSuffix;

                if (newName == baseName)
                {
                    Debug.Log($"[{baseName}] Name already correct, skipping.");
                    continue;
                }

                string err = AssetDatabase.RenameAsset(prefabPath, newName);
                if (!string.IsNullOrEmpty(err))
                    Debug.LogError($"[{baseName}] Rename error: {err}");
                else
                    Debug.Log($"[{baseName}] Renamed -> {newName}");
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
