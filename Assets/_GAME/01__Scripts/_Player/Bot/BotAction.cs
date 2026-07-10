using System;
using UnityEngine;

/// <summary>
/// One step in a bot's play sequence. A bot's whole performance is just an
/// ordered list of these (see <see cref="BotReplay"/>). The same format is used
/// for hand-authored bots and for sequences captured by the recorder, so both
/// approaches share one executor (<see cref="BotController"/>).
///
/// Actions are intentionally high-level / "intent" based (go to this position,
/// pull for this long) rather than raw per-frame stick input. Because the
/// executor drives toward a target state each step instead of replaying timed
/// input, small physics/framerate differences can't accumulate and derail the
/// run on these (non-deterministic) Unity physics levels.
/// </summary>
public enum BotActionType
{
    /// <summary>Walk to a world position, axis by axis, then stop.</summary>
    MoveToPosition,
    /// <summary>Tap jump once.</summary>
    Jump,
    /// <summary>Standard weapon hit.</summary>
    Hit,
    /// <summary>Downward weapon hit.</summary>
    HitDown,
    /// <summary>Special attack.</summary>
    Special,
    /// <summary>
    /// Latch the obstacle in front and pull until the player reaches
    /// <see cref="BotAction.targetPosition"/>, then release. Stops early if the
    /// obstacle is destroyed or the pull is aborted mid-drag. Position-based (not
    /// timed) so it can't drift when move speed varies with upgrades/powerups.
    /// </summary>
    Pull,
    /// <summary>Do nothing for <see cref="BotAction.duration"/> seconds.</summary>
    Wait,
}

[Serializable]
public class BotAction
{
    public BotActionType type;

    [Tooltip("Position (local to the level root) relevant to this action: the waypoint to walk to for " +
             "MoveToPosition, the player's end position for Pull, and the anchor where it fired for the " +
             "discrete actions (Jump/Hit/HitDown/Special) — the executor fires those at this position along " +
             "the movement leg, concurrently with the walk.")]
    public Vector3 targetPosition;

    [Tooltip("Idle time for Wait.")]
    public float duration;

    [Tooltip("'Thinking' pause before this action starts. Captured from the human's natural pauses when recording; jittered at playback for variety.")]
    public float preDelay;

    public BotAction() { }

    public BotAction(BotActionType type)
    {
        this.type = type;
    }
}
