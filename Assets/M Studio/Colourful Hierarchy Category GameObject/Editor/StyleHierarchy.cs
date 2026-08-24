using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace MStudio
{
    [InitializeOnLoad]
    public class StyleHierarchy
    {
        private static string[] dataArray;
        private static string path;
        private static ColorPalette colorPalette;
        private static Dictionary<char, ColorDesign> designCache;
        private static Dictionary<char, GUIStyle> styleCache;
        private static bool isInitialized = false;

        static StyleHierarchy()
        {
            Initialize();
            
            // Use Unity 6000 specific API if on Unity 6, otherwise fallback to standard integer API
            #if UNITY_6000_0_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyWindowByEntityId;
            #else
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindow;
            #endif
            
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private static void Initialize()
        {
            dataArray = AssetDatabase.FindAssets("t:ColorPalette");
            designCache = new Dictionary<char, ColorDesign>();
            styleCache = new Dictionary<char, GUIStyle>();

            if (dataArray.Length >= 1)
            {
                path = AssetDatabase.GUIDToAssetPath(dataArray[0]);
                colorPalette = AssetDatabase.LoadAssetAtPath<ColorPalette>(path);
                
                if (colorPalette != null)
                {
                    foreach (var design in colorPalette.colorDesigns)
                    {
                        if (!string.IsNullOrEmpty(design.keyChar) && !designCache.ContainsKey(design.keyChar[0]))
                        {
                            designCache.Add(design.keyChar[0], design);
                            
                            styleCache.Add(design.keyChar[0], new GUIStyle
                            {
                                alignment = design.textAlignment,
                                fontStyle = design.fontStyle,
                                normal = new GUIStyleState()
                                {
                                    textColor = design.textColor,
                                }
                            });
                        }
                    }
                    isInitialized = true;
                }
                else
                {
                    Debug.LogWarning("[StyleHierarchy] ColorPalette not found at path: " + path);
                    isInitialized = false;
                }
            }
            else
            {
                Debug.LogWarning("[StyleHierarchy] No ColorPalette asset found in the project.");
                isInitialized = false;
            }
        }

        private static void OnProjectChanged()
        {
            Initialize();
        }

        #if UNITY_6000_0_OR_NEWER
        // Unity 6+ uses the strongly typed EntityId struct
        private static void OnHierarchyWindowByEntityId(EntityId entityId, Rect selectionRect)
        {
            GameObject instance = EditorUtility.EntityIdToObject(entityId) as GameObject;
            ProcessHierarchyItem(instance, selectionRect);
        }
        #else
        // Older Unity versions use standard integer IDs
        private static void OnHierarchyWindow(int instanceID, Rect selectionRect)
        {
            GameObject instance = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            ProcessHierarchyItem(instance, selectionRect);
        }
        #endif

        // Now strictly handles the GameObject rather than trying to resolve the ID manually
        private static void ProcessHierarchyItem(GameObject instance, Rect selectionRect)
        {
            if (!isInitialized || colorPalette == null || designCache.Count == 0 || instance == null)
                return;

            string instanceName = instance.name;
            if (string.IsNullOrEmpty(instanceName))
                return;

            char firstChar = instanceName[0];
            
            if (designCache.TryGetValue(firstChar, out ColorDesign design) && 
                styleCache.TryGetValue(firstChar, out GUIStyle style))
            {
                string newName = instanceName.Length > 1 ? instanceName.Substring(1) : "";
                
                EditorGUI.DrawRect(selectionRect, design.backgroundColor);
                EditorGUI.LabelField(selectionRect, newName.ToUpper(), style);
            }
        }
    }
}