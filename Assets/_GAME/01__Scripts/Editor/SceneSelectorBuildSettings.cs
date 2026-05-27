using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneSelectorBuildSettings : EditorWindow
{
	private Vector2 _scrollPos;

	[MenuItem("Tools/Scene Selector (Build Settings)")]
	internal static void Init()
	{
		var window = (SceneSelectorBuildSettings)GetWindow(typeof(SceneSelectorBuildSettings), false, "Scene Selector (Build Settings)");
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

		EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
		int i = 0;
		foreach (EditorBuildSettingsScene scene in buildScenes)
		{
			if (string.IsNullOrEmpty(scene.path))
				continue;

			string sceneName = Path.GetFileNameWithoutExtension(scene.path);
			string label = i + ": " + sceneName + (scene.enabled ? "" : " (disabled)");

			var pressed = GUILayout.Button(label, new GUIStyle(GUI.skin.GetStyle("Button")) { alignment = TextAnchor.MiddleLeft });
			if (pressed)
			{
				if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				{
					EditorSceneManager.OpenScene(scene.path);
				}
			}
			i++;
		}

		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}
}
