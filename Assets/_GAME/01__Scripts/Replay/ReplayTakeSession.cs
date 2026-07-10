#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only handshake between the Tools/SWH/Replay menu and GameManager:
/// the menu sets <see cref="IsActive"/> and enters play mode; GameManager reads
/// it in Awake and runs the scene as a replay take session. Stored in
/// SessionState so it survives the play-mode domain reload; cleared by the menu
/// hook when play mode exits.
/// </summary>
public static class ReplayTakeSession
{
    private const string Key = "SWH.ReplayTake.Active";

    public static bool IsActive
    {
        get => SessionState.GetBool(Key, false);
        set => SessionState.SetBool(Key, value);
    }
}
#endif
