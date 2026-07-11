using System;
using System.Collections.Generic;
using Eflatun.SceneReference;
using UnityEngine;

/// <summary>
/// A state-based replay: a time map of everything needed to visually recreate a
/// recorded playthrough, played back kinematically (no simulation, no physics,
/// no gameplay logic — so no drift and no determinism requirements).
///
/// Positions are local to the level root (<see cref="ReplayScope"/> transform),
/// so a replay recorded on one half of an MP level plays back on the other half,
/// and levels can be moved/rotated without invalidating replays.
///
/// Player-only slice for now: player transform track + animator parameter events.
/// Entity tracks (obstacles), lifecycle events (spawn/destroy) and powerup events
/// are added in later steps. Plain serialized lists for debuggability; a binary
/// compression pass is planned once the format is proven.
/// </summary>
[CreateAssetMenu(fileName = "StateReplay", menuName = "SWH/State Replay")]
public class StateReplay : ScriptableObject
{
    [Tooltip("The level scene this replay was recorded in. Loaded by the bot-match fallback.")]
    public SceneReference scene;

    [Tooltip("Optional free-form id for the level, for grouping/filtering replays.")]
    public string levelId;

    [Tooltip("Free-form label, e.g. difficulty tier or who was recorded.")]
    public string label;

    [Tooltip("Total length of the recording in seconds.")]
    public float duration;

    [Tooltip("Seconds between player transform samples (playback interpolates between them).")]
    public float sampleInterval;

    public List<PlayerSample> playerTrack = new List<PlayerSample>();
    public List<AnimParamEvent> playerAnimTrack = new List<AnimParamEvent>();
    public List<ReplayEvent> events = new List<ReplayEvent>();
    public List<EntityTrack> entityTracks = new List<EntityTrack>();
    public List<ObstacleSpawnEvent> obstacleSpawnEvents = new List<ObstacleSpawnEvent>();
}

/// <summary>
/// Movement of one tracked entity (pushed/pulled/falling obstacle). Samples are
/// sparse — recorded only while the entity moves, with an anchor sample at each
/// movement start and a settle sample at each stop. Between bursts the position
/// at both ends is identical, so plain linear interpolation over the whole track
/// is correct (holds still through the gaps).
/// </summary>
[Serializable]
public class EntityTrack
{
    public int entityId;
    public List<EntitySample> samples = new List<EntitySample>();
}

/// <summary>One entity transform sample. Position local to the level root; yaw relative to the root's yaw.</summary>
[Serializable]
public struct EntitySample
{
    public float t;
    public Vector3 pos;
    public float yaw;
}

/// <summary>
/// An obstacle spawned during play (falling wave etc.). Playback instantiates a
/// visual replica: a template matching (type, color) — resolved from the level's
/// falling-obstacle prefab lists, falling back to cloning a scene instance — is
/// neutralized and registered under the given runtime entityId so subsequent
/// track samples and destroy events resolve to it.
/// </summary>
[Serializable]
public struct ObstacleSpawnEvent
{
    public float t;
    public int entityId;
    public ObstacleType obstacleType;
    public ObstacleColor obstacleColor;
    public Vector3 pos;
    public float yaw;
}

public enum ReplayEventKind
{
    /// <summary>The entity (obstacle) was destroyed. Playback runs Obstacle.ReplayDestroy (VFX + removal, no gameplay bookkeeping).</summary>
    ObstacleDestroyed = 0,
    /// <summary>
    /// The recorded player died (entityId unused). Playback plays the death
    /// animation on the ghost and reports it to the match arbiter — the bot
    /// dying is how the human wins a match against a losing recording.
    /// </summary>
    PlayerDied = 1,
    /// <summary>
    /// The rocket launched. Playback runs Rocket.ReplayLaunch (visuals only) —
    /// the flight path comes from the rocket's movement track and the obstacles
    /// it destroys from recorded destroy events, so the fired direction is exact
    /// without recording rotation.
    /// </summary>
    RocketLaunched = 2,
    /// <summary>
    /// A collectible was consumed. Playback runs CollectibleItem.ReplayCollect —
    /// pickup visuals only (vanish + sound); gameplay/on-ghost effects are
    /// deliberately not replayed (their outcomes are baked into the tracks).
    /// </summary>
    CollectibleCollected = 3,
    /// <summary>
    /// The recorded player's helmet took damage. Player-level event: entityId
    /// carries the damage amount instead of an entity reference. Replayed onto
    /// the ghost's helmet so cracks appear on schedule — and so a later helmet
    /// pickup's fly-to-head repair has something visible to repair.
    /// </summary>
    PlayerHelmetDamaged = 4,
}

/// <summary>A discrete state change on a tracked entity, resolved by <see cref="ReplayId"/> at playback.</summary>
[Serializable]
public struct ReplayEvent
{
    public float t;
    public int entityId;
    public ReplayEventKind kind;
}

/// <summary>One player transform sample. Position local to the level root; yaw relative to the root's yaw.</summary>
[Serializable]
public struct PlayerSample
{
    public float t;
    public Vector3 pos;
    public float yaw;
}

public enum AnimParamKind { Bool, Int, Float }

/// <summary>
/// An animator parameter change. We record the parameters the game sets (Idle,
/// Push, Pull, Hit, …) rather than clip state hashes: playback re-applies them and
/// lets the Animator run its own transitions, which is robust and tiny. Value is
/// stored as float for all kinds (bool → 0/1).
/// </summary>
[Serializable]
public struct AnimParamEvent
{
    public float t;
    public string param;
    public AnimParamKind kind;
    public float value;
}
