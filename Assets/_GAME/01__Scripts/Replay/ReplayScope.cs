using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The ID registry for one half of a multiplayer level. Place on the level root
/// (the same transform used as the replay's base transform — playerLevel /
/// opponentLevel). Every <see cref="ReplayId"/> under it registers here.
///
/// IDs are unique <b>within a scope</b>, not globally: the two halves of an MP
/// level are duplicates of the same layout, and corresponding entities carry the
/// <i>same</i> ID in each half. That's what lets a replay recorded on one half
/// (the recording scope) resolve onto the other (the playback scope) — record
/// writes "obstacle 12 destroyed at t=31.2", playback resolves 12 in its own
/// scope and gets the matching obstacle. Use the editor tools (Tools/SWH/Replay)
/// to assign IDs deterministically and mirror them between halves.
///
/// Entities created during play (falling obstacles, merged boxes) get runtime IDs
/// from <see cref="AllocateRuntimeId"/> — a range disjoint from authored IDs —
/// recorded in their spawn event and re-assigned at playback via
/// <see cref="ReplayId.AssignRuntime"/>.
/// </summary>
[DisallowMultipleComponent]
public class ReplayScope : MonoBehaviour
{
    /// <summary>First runtime-allocated ID. Authored (editor-assigned) IDs stay far below this.</summary>
    public const int RuntimeIdStart = 1_000_000;

    private readonly Dictionary<int, ReplayId> _entities = new Dictionary<int, ReplayId>();
    private int _nextRuntimeId = RuntimeIdStart;

    /// <summary>All currently registered entities, keyed by ID. Includes disabled ones (a destroyed obstacle stays resolvable).</summary>
    public IReadOnlyDictionary<int, ReplayId> Entities => _entities;

    public bool TryResolve(int id, out ReplayId entity) => _entities.TryGetValue(id, out entity);

    public ReplayId Resolve(int id)
    {
        if (_entities.TryGetValue(id, out ReplayId entity)) return entity;
        Debug.LogWarning($"[ReplayScope] '{name}' has no entity with id {id}. " +
                         "The replay was likely recorded against a different level layout — re-record it.", this);
        return null;
    }

    /// <summary>Next free ID for an entity spawned during play. Record it in the spawn event.</summary>
    public int AllocateRuntimeId() => _nextRuntimeId++;

    internal void Register(ReplayId entity)
    {
        if (_entities.TryGetValue(entity.Id, out ReplayId existing) && existing != null && existing != entity)
        {
            Debug.LogError($"[ReplayScope] Duplicate replay id {entity.Id} in scope '{name}': " +
                           $"'{existing.name}' and '{entity.name}'. Run Tools/SWH/Replay/Validate Replay IDs.", entity);
            return;
        }
        _entities[entity.Id] = entity;
    }

    internal void Unregister(ReplayId entity)
    {
        if (_entities.TryGetValue(entity.Id, out ReplayId existing) && existing == entity)
            _entities.Remove(entity.Id);
    }

    /// <summary>The scope a transform belongs to (nearest ReplayScope up the hierarchy), or null.</summary>
    public static ReplayScope FindScopeFor(Transform t) =>
        t != null ? t.GetComponentInParent<ReplayScope>(true) : null;

#if UNITY_EDITOR
    [Header("Editor tooling")]
    [Tooltip("Used by the 'Mirror Replay IDs From Source' context menu: IDs are copied from this scope onto " +
             "matching entities in this one (matched by hierarchy path). Set this on the opponent half and " +
             "point it at the player half (or vice versa).")]
    public ReplayScope mirrorSource;
#endif
}
