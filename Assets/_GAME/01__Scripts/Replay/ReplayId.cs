using UnityEngine;

/// <summary>
/// Stable identity for an entity the replay system tracks (obstacles,
/// collectibles, rockets, …). The ID is what binds recorded state — transform
/// tracks, destroy/spawn/collect events — to the right object at playback.
///
/// Two kinds of identity:
///  • <b>Authored</b>: assigned in-editor (Tools/SWH/Replay/Assign Replay IDs),
///    serialized, unique within the owning <see cref="ReplayScope"/>. Matching
///    entities in the two level halves carry the same ID (mirror tool).
///  • <b>Runtime</b>: for entities spawned during play. The recorder allocates an
///    ID from the scope (<see cref="ReplayScope.AllocateRuntimeId"/>) and calls
///    <see cref="AssignRuntime"/>; playback does the same with the ID stored in
///    the spawn event.
///
/// Registration lives from Awake to OnDestroy — deliberately not OnEnable/
/// OnDisable, because destroyed obstacles are deactivated rather than removed
/// and must stay resolvable (a destroy event references them by ID).
/// </summary>
[DisallowMultipleComponent]
public class ReplayId : MonoBehaviour
{
    [Tooltip("Unique within the owning ReplayScope. 0 = unassigned. Assign via Tools/SWH/Replay, don't hand-edit.")]
    [SerializeField] public int id;

    public int Id => id;

    /// <summary>The scope this entity is registered in (null until registered).</summary>
    public ReplayScope Scope { get; private set; }

    private void Awake()
    {
        // Runtime-spawned instances have id 0 here; they get registered later via
        // AssignRuntime by whoever spawned them (recorder / playback driver).
        if (id != 0) RegisterInParentScope();
    }

    private void OnDestroy()
    {
        if (Scope != null) Scope.Unregister(this);
        Scope = null;
    }

    private void RegisterInParentScope()
    {
        ReplayScope scope = ReplayScope.FindScopeFor(transform);
        if (scope == null)
        {
            Debug.LogWarning($"[ReplayId] '{name}' (id {id}) has no ReplayScope in its parents; " +
                             "it can't be resolved by replays. Put a ReplayScope on the level root.", this);
            return;
        }
        Scope = scope;
        scope.Register(this);
    }

    /// <summary>
    /// Gives a runtime-spawned entity its identity: the recorder calls this with a
    /// freshly allocated ID; playback calls it with the ID from the spawn event.
    /// Spawned entities are often instantiated unparented, so the scope is passed
    /// explicitly instead of resolved from the hierarchy.
    /// </summary>
    public void AssignRuntime(ReplayScope scope, int runtimeId)
    {
        if (Scope != null)
        {
            Debug.LogWarning($"[ReplayId] '{name}' already registered (id {id}); ignoring AssignRuntime({runtimeId}).", this);
            return;
        }
        id = runtimeId;
        Scope = scope;
        scope.Register(this);
    }

    /// <summary>
    /// The ReplayId governing a component (on it or a parent), or null. Use at
    /// record time to answer "what ID does this obstacle/collectible have".
    /// </summary>
    public static ReplayId Of(Component c) =>
        c != null ? c.GetComponentInParent<ReplayId>(true) : null;
}
