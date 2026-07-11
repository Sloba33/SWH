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
    private const string OverrideKey = "SWH.ReplayTake.OverrideStats";
    private const string MoveSpeedKey = "SWH.ReplayTake.MoveSpeed";
    private const string StrengthKey = "SWH.ReplayTake.Strength";

    public static bool IsActive
    {
        get => SessionState.GetBool(Key, false);
        set => SessionState.SetBool(Key, value);
    }

    // Stat override for the take, set by the Record Replay window. When armed,
    // Player.ApplyLocalPlayerStats uses these instead of the upgrade system's
    // values — so takes are made at known, tweakable stats.

    public static bool OverrideStats
    {
        get => SessionState.GetBool(OverrideKey, false);
        set => SessionState.SetBool(OverrideKey, value);
    }

    public static float MoveSpeed
    {
        get => SessionState.GetFloat(MoveSpeedKey, 2f);
        set => SessionState.SetFloat(MoveSpeedKey, value);
    }

    public static float Strength
    {
        get => SessionState.GetFloat(StrengthKey, 10f);
        set => SessionState.SetFloat(StrengthKey, value);
    }
}
#endif
