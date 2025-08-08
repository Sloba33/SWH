using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public class MetalBoxReplacerWindow : EditorWindow
{
    public GameObject metalBoxXPrefab;
    public GameObject metalBoxOPrefab;

    public List<SceneAsset> scenesToFix = new();

    [MenuItem("Tools/Metal Box Replacer")]
    public static void ShowWindow()
    {
        GetWindow<MetalBoxReplacerWindow>("Metal Box Replacer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Metal Box Prefabs", EditorStyles.boldLabel);
        metalBoxXPrefab = (GameObject)EditorGUILayout.ObjectField("X Version", metalBoxXPrefab, typeof(GameObject), false);
        metalBoxOPrefab = (GameObject)EditorGUILayout.ObjectField("O Version", metalBoxOPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scenes to Fix", EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(this);
        SerializedProperty sceneListProp = so.FindProperty("scenesToFix");
        EditorGUILayout.PropertyField(sceneListProp, true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix Selected Scenes"))
        {
            FixScenes();
        }
    }

    private void FixScenes()
    {
        if (metalBoxXPrefab == null || metalBoxOPrefab == null)
        {
            Debug.LogError("Please assign both metalBoxX and metalBoxO prefabs.");
            return;
        }

        foreach (SceneAsset sceneAsset in scenesToFix)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            if (string.IsNullOrEmpty(path)) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool modified = false;

            foreach (Obstacle obs in GameObject.FindObjectsOfType<Obstacle>())
            {
                if (obs.obstacleType == ObstacleType.Metal && obs.is_x_box)
                {
                    Vector3 pos = obs.transform.position;
                    Quaternion rot = obs.transform.rotation;
                    Transform parent = obs.transform.parent;

                    GameObject newBox = (GameObject)PrefabUtility.InstantiatePrefab(metalBoxOPrefab);
                    newBox.transform.SetPositionAndRotation(pos, rot);
                    if (parent != null) newBox.transform.SetParent(parent);

                    DestroyImmediate(obs.gameObject);
                    modified = true;
                }
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Fixed scene: " + path);
            }
            else
            {
                Debug.Log("No replacements needed in: " + path);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Replacement complete.");
    }
}
