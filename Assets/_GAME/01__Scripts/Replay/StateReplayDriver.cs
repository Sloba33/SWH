using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the player track of a <see cref="StateReplay"/> on a character GameObject
/// as a kinematic puppet: transform set from interpolated recorded samples,
/// animator driven by recorded parameter events. No physics, no gameplay code,
/// no input — which is exactly why it cannot drift or desync.
///
/// Use <see cref="AttachGhost"/> to turn a freshly instantiated character into a
/// ghost: it destroys the cameras, disables every gameplay MonoBehaviour and all
/// colliders (a ghost must not push obstacles, trip triggers, or collect
/// pickups), makes rigidbodies kinematic, then attaches and starts this driver.
/// The Animator (not a MonoBehaviour) stays enabled — the driver owns its
/// parameters, replacing the disabled PlayerAnimation.
/// </summary>
public class StateReplayDriver : MonoBehaviour
{
    public StateReplay replay;

    [Tooltip("Level root (ReplayScope transform) the replay's positions are relative to. " +
             "The opponent half in a bot match; the recording half when overlay-testing.")]
    public Transform levelRoot;

    private Animator _animator;
    private ReplayScope _scope; // playback scope: resolves event entityIds on this half
    private bool _playing;
    private float _time;
    private int _cursor;       // index into playerTrack: last sample with t <= _time
    private int _animCursor;   // next anim event to apply
    private int _eventCursor;  // next entity event to apply
    private int _spawnCursor;  // next obstacle spawn event to apply

    // Playback state for one entity movement track.
    private class TrackState
    {
        public EntityTrack track;
        public int cursor;
        public Transform target;     // resolved lazily (spawned entities appear mid-replay)
        public bool resolvedOnce;    // resolved then Unity-null again ⇒ destroyed, track done
    }
    private readonly List<TrackState> _trackStates = new List<TrackState>();

    public bool IsPlaying => _playing;
    public bool IsFinished { get; private set; }

    /// <summary>
    /// The recorded player died at this point of the replay. The bot-match
    /// arbiter subscribes: a dead bot means the human wins.
    /// </summary>
    public event System.Action ReplayPlayerDied;
    public float Time01 => replay != null && replay.duration > 0f ? Mathf.Clamp01(_time / replay.duration) : 0f;

    /// <summary>
    /// Converts an instantiated character into a replay ghost and starts playback.
    /// Call right after Instantiate, before the character's Start methods run, so
    /// disabled gameplay components never initialize.
    /// </summary>
    public static StateReplayDriver AttachGhost(GameObject character, StateReplay replay, Transform levelRoot, bool autoPlay = true)
    {
        NeutralizeCharacter(character);

        StateReplayDriver driver = character.AddComponent<StateReplayDriver>();
        driver.replay = replay;
        driver.levelRoot = levelRoot;
        if (autoPlay) driver.Play();
        return driver;
    }

    /// <summary>
    /// Strips a character of everything except visuals: cameras destroyed (a ghost
    /// is never the viewpoint — mirrors the MP remote-player teardown), every
    /// MonoBehaviour disabled, colliders off, rigidbodies kinematic.
    /// </summary>
    private static void NeutralizeCharacter(GameObject character)
    {
        PlayerController pc = character.GetComponent<PlayerController>();
        if (pc != null)
        {
            if (pc.playerCamera != null) Destroy(pc.playerCamera.gameObject);
            if (pc.followCamera != null) Destroy(pc.followCamera.gameObject);
        }
        NeutralizeEntity(character);
    }

    /// <summary>
    /// Freezes the replayed half of the level for a bot match: every obstacle
    /// under the root goes kinematic with colliders off, so live physics can
    /// never move it — the replay (movement tracks + destroy events) is the only
    /// thing that drives that half. Without this, authored at-height obstacles
    /// on the bot side free-fall under gravity at match start.
    ///
    /// MonoBehaviours are left enabled on purpose: BotMatchArbiter counts the
    /// bot's goal by isActiveAndEnabled, and ReplayDestroy works either way.
    /// </summary>
    public static void FreezeReplayHalf(Transform root)
    {
        if (root == null) return;
        foreach (Obstacle obs in root.GetComponentsInChildren<Obstacle>(true))
        {
            Rigidbody rb = obs.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            foreach (Collider col in obs.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
        }
    }

    /// <summary>
    /// Turns any GameObject into a pure visual puppet: every MonoBehaviour
    /// disabled, colliders off, rigidbodies kinematic. Used for the ghost
    /// character and for spawned obstacle replicas.
    /// </summary>
    public static void NeutralizeEntity(GameObject go)
    {
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null) mb.enabled = false; // null: missing-script stubs
        }
        foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }
        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
        }
    }

    public void Play()
    {
        if (replay == null || replay.playerTrack.Count == 0)
        {
            Debug.LogWarning("[StateReplayDriver] No replay (or empty player track); nothing to play.", this);
            return;
        }
        _animator = GetComponent<Animator>();
        _scope = levelRoot != null ? levelRoot.GetComponent<ReplayScope>() : null;
        if (_scope == null && replay.events.Count > 0)
            Debug.LogWarning("[StateReplayDriver] Replay has entity events but the level root has no ReplayScope — " +
                             "obstacle destruction will not play back.", this);
        _time = 0f;
        _cursor = 0;
        _animCursor = 0;
        _eventCursor = 0;
        _spawnCursor = 0;
        _trackStates.Clear();
        foreach (EntityTrack track in replay.entityTracks)
            _trackStates.Add(new TrackState { track = track });
        IsFinished = false;
        _playing = true;
        ApplyPose(); // snap to the recorded start immediately
        ApplyAnimEventsUpTo(0f);
        ApplySpawnEventsUpTo(0f);
        ApplyEntityEventsUpTo(0f);
        ApplyEntityTracks();
    }

    public void Stop()
    {
        _playing = false;
    }

    private void Update()
    {
        if (!_playing) return;

        _time += UnityEngine.Time.deltaTime;
        ApplyAnimEventsUpTo(_time);
        ApplySpawnEventsUpTo(_time);  // spawns before events: a same-tick destroy must find the entity
        ApplyEntityEventsUpTo(_time);
        ApplyEntityTracks();
        ApplyPose();

        if (_time >= replay.duration)
        {
            _playing = false;
            IsFinished = true;
        }
    }

    private void ApplyPose()
    {
        var track = replay.playerTrack;

        while (_cursor < track.Count - 1 && track[_cursor + 1].t <= _time) _cursor++;

        PlayerSample a = track[_cursor];
        Vector3 pos;
        float yaw;

        if (_cursor >= track.Count - 1)
        {
            pos = a.pos;
            yaw = a.yaw;
        }
        else
        {
            PlayerSample b = track[_cursor + 1];
            float span = b.t - a.t;
            float f = span > 1e-6f ? Mathf.Clamp01((_time - a.t) / span) : 1f;
            pos = Vector3.Lerp(a.pos, b.pos, f);
            yaw = Mathf.LerpAngle(a.yaw, b.yaw, f);
        }

        transform.position = ToWorld(pos);
        transform.rotation = Quaternion.Euler(0f, RootYaw() + yaw, 0f);
    }

    private void ApplyAnimEventsUpTo(float t)
    {
        if (_animator == null) return;
        var events = replay.playerAnimTrack;
        while (_animCursor < events.Count && events[_animCursor].t <= t)
        {
            AnimParamEvent e = events[_animCursor++];
            switch (e.kind)
            {
                case AnimParamKind.Bool: _animator.SetBool(e.param, e.value > 0.5f); break;
                case AnimParamKind.Int: _animator.SetInteger(e.param, Mathf.RoundToInt(e.value)); break;
                case AnimParamKind.Float: _animator.SetFloat(e.param, e.value); break;
            }
        }
    }

    private void ApplyEntityEventsUpTo(float t)
    {
        var events = replay.events;
        while (_eventCursor < events.Count && events[_eventCursor].t <= t)
        {
            ApplyEntityEvent(events[_eventCursor++]);
        }
    }

    private void ApplyEntityEvent(ReplayEvent e)
    {
        // Player-level events carry no entityId — handle before resolution.
        if (e.kind == ReplayEventKind.PlayerDied)
        {
            if (_animator != null) _animator.Play("Death_Animation"); // same clip Player.Die plays
            ReplayPlayerDied?.Invoke();
            return;
        }

        if (_scope == null) return;
        if (!_scope.TryResolve(e.entityId, out ReplayId entity) || entity == null)
        {
            Debug.LogWarning($"[StateReplayDriver] Event at t={e.t:F1} references unknown entity id {e.entityId} — " +
                             "level layout likely changed since this replay was recorded.", this);
            return;
        }

        switch (e.kind)
        {
            case ReplayEventKind.ObstacleDestroyed:
                Obstacle obstacle = entity.GetComponent<Obstacle>();
                if (obstacle != null) obstacle.ReplayDestroy();
                else entity.gameObject.SetActive(false); // fallback: at least make it vanish
                break;
        }
    }

    // ------------------------------------------------------ entity movement

    /// <summary>
    /// Drives every entity movement track: resolve the target (lazily — spawned
    /// entities only exist after their spawn event), advance to the current time,
    /// interpolate, set the transform. A resolved-then-null target means the
    /// entity was destroyed by a destroy event; its track is simply done.
    /// </summary>
    private void ApplyEntityTracks()
    {
        foreach (TrackState ts in _trackStates)
        {
            if (ts.target == null)
            {
                if (ts.resolvedOnce) continue; // destroyed — done
                if (_scope == null || !_scope.TryResolve(ts.track.entityId, out ReplayId entity) || entity == null)
                    continue; // not spawned yet (or unknown id — spawn/destroy paths warn already)
                ts.target = entity.transform;
                ts.resolvedOnce = true;
            }

            var samples = ts.track.samples;
            if (samples.Count == 0) continue;

            while (ts.cursor < samples.Count - 1 && samples[ts.cursor + 1].t <= _time) ts.cursor++;

            EntitySample a = samples[ts.cursor];
            Vector3 pos;
            float yaw;
            if (ts.cursor >= samples.Count - 1 || a.t > _time)
            {
                // Past the end, or before the first sample (entity at rest).
                pos = a.t > _time ? samples[0].pos : a.pos;
                yaw = a.t > _time ? samples[0].yaw : a.yaw;
            }
            else
            {
                EntitySample b = samples[ts.cursor + 1];
                float span = b.t - a.t;
                float f = span > 1e-6f ? Mathf.Clamp01((_time - a.t) / span) : 1f;
                pos = Vector3.Lerp(a.pos, b.pos, f);
                yaw = Mathf.LerpAngle(a.yaw, b.yaw, f);
            }

            ts.target.position = ToWorld(pos);
            ts.target.rotation = Quaternion.Euler(0f, RootYaw() + yaw, 0f);
        }
    }

    // ------------------------------------------------------ obstacle spawns

    private void ApplySpawnEventsUpTo(float t)
    {
        var spawns = replay.obstacleSpawnEvents;
        while (_spawnCursor < spawns.Count && spawns[_spawnCursor].t <= t)
        {
            ApplySpawn(spawns[_spawnCursor++]);
        }
    }

    /// <summary>
    /// Recreates a runtime-spawned obstacle as a neutralized visual replica,
    /// registered under the recorded runtime id so its track samples and destroy
    /// event resolve to it.
    /// </summary>
    private void ApplySpawn(ObstacleSpawnEvent e)
    {
        if (_scope == null) return;

        Obstacle template = FindSpawnTemplate(e.obstacleType, e.obstacleColor);
        if (template == null)
        {
            Debug.LogWarning($"[StateReplayDriver] No template found for spawned obstacle " +
                             $"{e.obstacleType}-{e.obstacleColor} (t={e.t:F1}); spawn skipped.", this);
            return;
        }

        Obstacle replica = Instantiate(template, ToWorld(e.pos),
            Quaternion.Euler(0f, RootYaw() + e.yaw, 0f), _scope.transform);
        replica.name = $"ReplaySpawn_{e.obstacleType}_{e.obstacleColor}_{e.entityId}";
        replica.gameObject.SetActive(true); // scene-instance templates may be a disabled source
        NeutralizeEntity(replica.gameObject);

        // Cloned scene instances can carry a serialized authored id — strip it so
        // AssignRuntime can register the recorded runtime id cleanly.
        ReplayId existing = replica.GetComponent<ReplayId>();
        if (existing != null) Destroy(existing);
        ReplayId rid = replica.gameObject.AddComponent<ReplayId>();
        rid.AssignRuntime(_scope, e.entityId);
    }

    /// <summary>
    /// Template for a spawned obstacle: the level's falling-obstacle prefab lists
    /// first (exactly what the recording spawned from), then any scene instance of
    /// the same type/color in the playback scope as a fallback.
    /// </summary>
    private Obstacle FindSpawnTemplate(ObstacleType type, ObstacleColor color)
    {
        LevelGoal levelGoal = GameManager.Instance != null ? GameManager.Instance.levelGoal : null;
        if (levelGoal != null)
        {
            foreach (var item in levelGoal.FallingObstacles)
            {
                if (item.item != null && item.item.obstacleType == type && item.item.obstacleColor == color)
                    return item.item;
            }
            foreach (var item in levelGoal.fixedFallingObstacles)
            {
                if (item.item != null && item.item.obstacleType == type && item.item.obstacleColor == color)
                    return item.item;
            }
        }

        if (_scope != null)
        {
            foreach (var kvp in _scope.Entities)
            {
                if (kvp.Value == null) continue;
                Obstacle obs = kvp.Value.GetComponent<Obstacle>();
                if (obs != null && obs.obstacleType == type && obs.obstacleColor == color)
                    return obs;
            }
        }
        return null;
    }

    private Vector3 ToWorld(Vector3 local) => levelRoot != null ? levelRoot.TransformPoint(local) : local;
    private float RootYaw() => levelRoot != null ? levelRoot.eulerAngles.y : 0f;
}
