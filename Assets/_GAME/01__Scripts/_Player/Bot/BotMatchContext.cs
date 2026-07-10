/// <summary>
/// Carries the chosen <see cref="StateReplay"/> from the matchmaking scene into the
/// freshly-loaded level scene. The matchmaking controller sets
/// <see cref="PendingReplay"/> right before it loads the level on bot fallback;
/// <c>GameManager</c> reads it in Awake to decide between a Coherence-connected
/// match and a local bot match, then clears it.
///
/// Static (rather than a DontDestroyOnLoad carrier) because it only needs to hold
/// one value across a single scene transition, mirroring how
/// <see cref="MultiplayerMatchmakingController"/> already passes skill values via
/// static fields. A StateReplay is a ScriptableObject asset, so the reference stays
/// valid across the load.
/// </summary>
public static class BotMatchContext
{
    public static StateReplay PendingReplay;

    /// <summary>True when the level about to run (or running) is a local bot match.</summary>
    public static bool IsBotMatch => PendingReplay != null;

    public static void Clear() => PendingReplay = null;
}
