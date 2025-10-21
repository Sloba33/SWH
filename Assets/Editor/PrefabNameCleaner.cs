#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class PrefabNameCleaner : EditorWindow
{
    public List<GameObject> prefabs = new List<GameObject>();

    [MenuItem("Tools/Cleanup Prefab Names")]
    public static void ShowWindow()
    {
        GetWindow<PrefabNameCleaner>("Cleanup Prefab Names");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);
        EditorGUILayout.PropertyField(so.FindProperty("prefabs"), true);
        so.ApplyModifiedProperties();

        if (GUILayout.Button("🧹 Clean Prefab Names"))
        {
            CleanNames();
        }
    }

    private void CleanNames()
    {
        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;

            string path = AssetDatabase.GetAssetPath(prefab);
            string originalName = Path.GetFileNameWithoutExtension(path);
            string name = originalName;

            // Step 1: Remove all "_Unmatched" fragments completely
            name = Regex.Replace(name, "_Unmatched", "", RegexOptions.IgnoreCase);

            // Step 2: If multiple "_Matched" exist, keep only the last one
            int firstMatchedIndex = name.IndexOf("_Matched", System.StringComparison.OrdinalIgnoreCase);
            if (firstMatchedIndex != -1)
            {
                // Split into before and after _Matched
                string before = name.Substring(0, firstMatchedIndex);
                string after = name.Substring(firstMatchedIndex + "_Matched".Length);

                // Clean repeated _Matched or stray underscores
                after = after.TrimStart('_').Replace("_Matched", "", System.StringComparison.OrdinalIgnoreCase);

                name = $"{before}_Matched";
                if (!string.IsNullOrWhiteSpace(after))
                    name += $"_{after.Trim()}";
            }

            // Step 3: Collapse multiple underscores and trim
            name = Regex.Replace(name, "_{2,}", "_").TrimEnd('_').Trim();

            // Step 4: Rename asset if changed
            if (name != originalName)
            {
                string err = AssetDatabase.RenameAsset(path, name);
                if (!string.IsNullOrEmpty(err))
                    Debug.LogError($"[Cleaner] Rename error for {originalName}: {err}");
                else
                    Debug.Log($"[Cleaner] ✅ {originalName} → {name}");
            }
            else
            {
                Debug.Log($"[Cleaner] (No change) {originalName}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
