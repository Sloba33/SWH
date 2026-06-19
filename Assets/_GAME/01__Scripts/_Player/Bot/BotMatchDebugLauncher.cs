using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug-only entry point for starting a local bot match without going through
/// matchmaking. Drop this on a GameObject in a throwaway debug scene, assign a
/// <see cref="BotReplay"/>, and press play: it sets the same static handoff the
/// matchmaking bot-fallback uses (<see cref="BotMatchContext.PendingReplay"/>)
/// and loads the target scene, so GameManager spins up the bot match exactly as
/// it would in production.
///
/// See <c>BotMatchDebugLauncherEditor</c> for the inspector buttons / replay picker.
/// </summary>
public class BotMatchDebugLauncher : MonoBehaviour
{
    [Tooltip("Replay to play. Its own scene is loaded unless a Scene Override is set below.")]
    public BotReplay replay;

    [Tooltip("Optional: load this scene instead of the one stored on the replay.")]
    public SceneReference sceneOverride;

    [Tooltip("Launch automatically when this debug scene enters play mode.")]
    public bool launchOnStart = true;

    private void Start()
    {
        if (launchOnStart) Launch();
    }

    /// <summary>
    /// Sets the bot-match context and loads the target scene — the same two steps
    /// MultiplayerMatchmakingController performs on bot fallback.
    /// </summary>
    public void Launch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[BotMatchDebugLauncher] Launch only works in play mode.");
            return;
        }
        if (replay == null)
        {
            Debug.LogError("[BotMatchDebugLauncher] No replay assigned.");
            return;
        }
        string sceneName = ResolveSceneName();
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[BotMatchDebugLauncher] No valid scene to load. Assign a Scene Override or set a scene on the replay. " +
                           "(The scene must also be in Build Settings.)");
            return;
        }

        BotMatchContext.PendingReplay = replay;
        Debug.Log($"[BotMatchDebugLauncher] Launching bot match: replay '{replay.name}' in scene '{sceneName}'.");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>The scene that <see cref="Launch"/> will load: the override if valid, else the replay's scene.</summary>
    public string ResolveSceneName()
    {
        if (sceneOverride != null && sceneOverride.UnsafeReason == SceneReferenceUnsafeReason.None)
            return sceneOverride.Name;
        if (replay != null && replay.scene != null && replay.scene.UnsafeReason == SceneReferenceUnsafeReason.None)
            return replay.scene.Name;
        return null;
    }
}
