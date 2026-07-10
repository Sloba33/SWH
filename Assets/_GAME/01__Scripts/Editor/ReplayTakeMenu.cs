using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click replay recording for designers/artists: open the level scene you
/// want a bot replay for and run the menu item. It enters play mode as a
/// "take session" (see ReplayTakeSession / ReplayTakeController): the level
/// starts frozen, recording begins on your first action, and the save dialog
/// appears automatically when you win or die. No scene changes required.
/// </summary>
public static class ReplayTakeMenu
{
    private const string MenuPath = "Tools/SWH/Replay/Record Replay In This Scene";

    [MenuItem(MenuPath, priority = 0)]
    private static void StartTake()
    {
        ReplayTakeSession.IsActive = true;
        EditorApplication.EnterPlaymode();
    }

    [MenuItem(MenuPath, validate = true)]
    private static bool ValidateStartTake() => !EditorApplication.isPlaying;

    // Clear the flag whenever play mode ends so a later normal play session
    // can't accidentally start as a take.
    [InitializeOnLoadMethod]
    private static void InstallCleanupHook()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                ReplayTakeSession.IsActive = false;
        };
    }
}
