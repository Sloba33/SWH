using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;

public class FacebookSettingsGenerator
{
    [MenuItem("Tools/Generate Facebook Settings")]
    public static void GenerateSettings()
    {
        try
        {
            // Try to find the FacebookSettings type via reflection
            var settingsType = System.Type.GetType("Facebook.Unity.Settings.FacebookSettings, Facebook.Unity.Settings");
            
            if (settingsType == null)
            {
                Debug.LogError("[Facebook] Could not find FacebookSettings type. Make sure the SDK is properly imported.");
                return;
            }

            // Try to call GetOrCreateSettings
            var method = settingsType.GetMethod("GetOrCreateSettings", BindingFlags.Public | BindingFlags.Static);
            
            if (method != null)
            {
                var settings = method.Invoke(null, null);
                Debug.Log("[Facebook] Settings created successfully!");
                AssetDatabase.Refresh();
                
                // Find the asset path
                var findMethod = settingsType.GetMethod("FindSettings", BindingFlags.NonPublic | BindingFlags.Static);
                if (findMethod != null)
                {
                    var path = findMethod.Invoke(null, null) as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Debug.Log($"[Facebook] Settings located at: {path}");
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
                    }
                }
            }
            else
            {
                Debug.LogError("[Facebook] Could not find GetOrCreateSettings method.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Facebook] Error generating settings: {e.Message}");
        }
    }
}