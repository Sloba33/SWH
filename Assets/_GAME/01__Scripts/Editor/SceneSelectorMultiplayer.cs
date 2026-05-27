using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public class SceneSelectorMultiplayer : EditorWindow
{
	private Vector2 _scrollPos;

	[MenuItem("Tools/Scene Selector (Multiplayer)")]
	internal static void Init()
	{
		var window = (SceneSelectorMultiplayer)GetWindow(typeof(SceneSelectorMultiplayer), false, "Scene Selector (Multiplayer)");
		window.position = new Rect(window.position.xMin + 100f, window.position.yMin + 100f, 200f, 400f);
	}

	/// <summary>
	/// Called on GUI events.
	/// </summary>
	internal void OnGUI()
	{
		EditorGUILayout.BeginVertical();
		_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, false);
		GUILayout.Label("Scenes", EditorStyles.boldLabel);

		int i = 0;
		string sceneFolder = Application.dataPath + "/_GAME/03__Scenes/";
		// find all scenes in the folder
		foreach (string scene in Directory.GetFiles(sceneFolder, "*", SearchOption.TopDirectoryOnly))
		{
			if (!scene.EndsWith(".unity"))
				continue;
			string sceneName = Path.GetFileNameWithoutExtension(scene);
			// convert absolute to relative path
			string relativePath = "Assets" + scene.Substring(Application.dataPath.Length);

            if (relativePath.StartsWith("Assets/Scenes/Test\\"))
            {
                continue;
            }

			// create a button for each scene
			var pressed = GUILayout.Button(i + ": " + sceneName, new GUIStyle(GUI.skin.GetStyle("Button")) { alignment = TextAnchor.MiddleLeft });
			if (pressed)
			{
				if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				{
					EditorSceneManager.OpenScene(relativePath);
				}
			}
			i++;
		}

		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}
}