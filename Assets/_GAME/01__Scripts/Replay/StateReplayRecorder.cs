using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Records a live playthrough into a <see cref="StateReplay"/>. Player-only slice:
/// samples the target player's transform at a fixed rate and captures every
/// animator parameter change (bool/int/float; triggers are not supported — the
/// player rig uses bools). Strictly read-only: it observes the live game, never
/// mutates it.
///
/// Positions are stored local to <see cref="levelRoot"/> (the ReplayScope
/// transform of the half being played), matching playback. Sampling happens in
/// FixedUpdate — gameplay is physics-driven, so state changes on fixed steps.
///
/// Saving to an asset is done by the editor (StateReplayRecorderEditor) via
/// <see cref="PopulateReplay"/>.
/// </summary>
public class StateReplayRecorder : MonoBehaviour
{
    [Tooltip("Player to record. If null, resolves to the local human player on Start Recording.")]
    public Player target;

    [Tooltip("Level root (ReplayScope transform) the recording is relative to. Must correspond to the " +
             "scope the replay will play back under (the opponent half).")]
    public Transform levelRoot;

    [Tooltip("Begin recording automatically when play starts.")]
    public bool recordOnStart;

    [Tooltip("Player transform samples per second. Playback interpolates, so 20-30 is plenty for grid movement.")]
    [Range(5f, 50f)] public float sampleRate = 30f;

    private bool _recording;
    private float _time;
    private float _nextSampleTime;
    private float _sampleInterval;

    private Animator _animator;
    private string _scenePath;
    private ReplayScope _scope;

    private List<PlayerSample> _playerTrack;
    private List<AnimParamEvent> _animTrack;
    private List<ReplayEvent> _events;
    private List<ObstacleSpawnEvent> _spawnEvents;

    // Per-entity movement capture. Samples are emitted only while the entity
    // moves: an anchor sample when movement starts (previous pose), then one per
    // sample tick, then a settle sample when it stops.
    private class TrackBuilder
    {
        public int id;
        public Transform tf;
        public List<EntitySample> samples = new List<EntitySample>();
        public Vector3 lastPos;   // local
        public float lastYaw;     // relative to root
        public float lastTime;
        public bool moving;
    }
    private readonly List<TrackBuilder> _tracks = new List<TrackBuilder>();

    private const float MoveEpsilonSq = 0.0004f * 0.0004f; // 0.4 mm
    private const float YawEpsilon = 0.25f;                // degrees

    // XZ footprint (levelRoot-local) of the recorded half, derived from its
    // tiles. Spawns outside it belong to the other half / elsewhere and are not
    // part of this performance.
    private bool _hasHalfBounds;
    private float _minX, _maxX, _minZ, _maxZ;
    private const float HalfBoundsMargin = 1.5f;

    // Cached animator parameter states for change detection.
    private class ParamState
    {
        public string name;
        public AnimParamKind kind;
        public float last;
    }
    private readonly List<ParamState> _params = new List<ParamState>();

    public bool IsRecording => _recording;
    public float RecordedSeconds => _time;
    public int PlayerSampleCount => _playerTrack != null ? _playerTrack.Count : 0;
    public int AnimEventCount => _animTrack != null ? _animTrack.Count : 0;
    public int EventCount => _events != null ? _events.Count : 0;

    private void Start()
    {
        if (recordOnStart) StartRecording();
    }

    public void StartRecording()
    {
        if (_recording) return;
        if (target == null) target = ResolveTarget();
        if (target == null)
        {
            Debug.LogError("[StateReplayRecorder] No target Player to record.");
            return;
        }

        _animator = target.GetComponent<Animator>();
        _scenePath = SceneManager.GetActiveScene().path;
        _playerTrack = new List<PlayerSample>();
        _animTrack = new List<AnimParamEvent>();
        _events = new List<ReplayEvent>();
        _time = 0f;
        _sampleInterval = 1f / sampleRate;
        _nextSampleTime = 0f;

        _scope = levelRoot != null ? levelRoot.GetComponent<ReplayScope>() : null;
        if (_scope == null)
            Debug.LogWarning("[StateReplayRecorder] Level Root has no ReplayScope — entity events (obstacle " +
                             "destruction/spawn) and movement tracks will NOT be recorded, only the player track.");
        Obstacle.ObstacleDestroyed += OnObstacleDestroyed;
        Obstacle.ObstacleSpawned += OnObstacleSpawned;
        Player.PlayerDied += OnPlayerDied;
        _deathRecorded = false;

        ComputeHalfBounds();

        _spawnEvents = new List<ObstacleSpawnEvent>();
        _tracks.Clear();
        if (_scope != null)
        {
            foreach (var kvp in _scope.Entities)
            {
                if (kvp.Value == null || kvp.Value.GetComponent<Obstacle>() == null) continue;
                StartTracking(kvp.Value);
            }
        }

        CacheAnimParams();
        EmitInitialAnimSnapshot();
        SamplePlayer(); // t = 0

        _recording = true;
        Debug.Log("[StateReplayRecorder] Recording started.");
    }

    public void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        Obstacle.ObstacleDestroyed -= OnObstacleDestroyed;
        Obstacle.ObstacleSpawned -= OnObstacleSpawned;
        Player.PlayerDied -= OnPlayerDied;
        SamplePlayer(); // final pose
        foreach (TrackBuilder tb in _tracks)
        {
            if (tb.moving && tb.tf != null) EmitEntitySample(tb); // settle
        }
        Debug.Log($"[StateReplayRecorder] Recording stopped: {_time:F1}s, {PlayerSampleCount} samples, " +
                  $"{AnimEventCount} anim events, {EventCount} entity events, {_spawnEvents.Count} spawns, " +
                  $"{CountMovingTracks()} moving entities.");
    }

    private void OnDestroy()
    {
        Obstacle.ObstacleDestroyed -= OnObstacleDestroyed;
        Obstacle.ObstacleSpawned -= OnObstacleSpawned;
        Player.PlayerDied -= OnPlayerDied;
    }

    private bool _deathRecorded;

    /// <summary>
    /// The recorded player died. Captured as a replay event so the ghost dies at
    /// the same moment — which is how a losing recording loses the bot match.
    /// Deduped: Die can fire more than once (e.g. trap + bomb the same frame).
    /// </summary>
    private void OnPlayerDied(Player who)
    {
        if (!_recording || _deathRecorded || who != target) return;
        _deathRecorded = true;
        _events.Add(new ReplayEvent { t = _time, entityId = 0, kind = ReplayEventKind.PlayerDied });
    }

    private int CountMovingTracks()
    {
        int n = 0;
        foreach (TrackBuilder tb in _tracks) if (tb.samples.Count > 0) n++;
        return n;
    }

    /// <summary>
    /// Raised by Obstacle.ParticleDestroy the moment a destruction commits, before
    /// the GameObject is torn down — so the ReplayId is still resolvable.
    /// </summary>
    private void OnObstacleDestroyed(Obstacle obstacle)
    {
        if (!_recording) return;

        ReplayId rid = ReplayId.Of(obstacle);
        if (rid == null || rid.Id == 0)
        {
            Debug.LogWarning($"[StateReplayRecorder] Destroyed obstacle '{obstacle.name}' has no ReplayId — " +
                             "not recorded. Run Tools/SWH/Replay/Assign Replay IDs.", obstacle);
            return;
        }
        // Only record entities of the half being recorded — destructions elsewhere
        // (the other half in an MP scene) are not part of this performance.
        if (_scope != null && rid.Scope != _scope) return;

        _events.Add(new ReplayEvent { t = _time, entityId = rid.Id, kind = ReplayEventKind.ObstacleDestroyed });
    }

    private void FixedUpdate()
    {
        if (!_recording || target == null) return;
        _time += Time.fixedDeltaTime;

        // Animator params are compared every fixed step (not just at sample rate)
        // so short-lived bool flips (a quick Hit) are never missed.
        CaptureAnimChanges();

        if (_time >= _nextSampleTime)
        {
            SamplePlayer();
            SampleEntities();
            _nextSampleTime += _sampleInterval;
        }
    }

    /// <summary>
    /// Event-gated movement capture: nothing is written for an entity while it
    /// holds still. On movement start, an anchor sample of the previous pose is
    /// emitted first so playback interpolates from the true rest position; on
    /// stop, a settle sample pins the exact final pose.
    /// </summary>
    private void SampleEntities()
    {
        foreach (TrackBuilder tb in _tracks)
        {
            if (tb.tf == null) continue; // destroyed mid-recording — track is complete

            Vector3 pos = ToLocal(tb.tf.position);
            float yaw = tb.tf.eulerAngles.y - RootYaw();
            bool moved = (pos - tb.lastPos).sqrMagnitude > MoveEpsilonSq
                         || Mathf.Abs(Mathf.DeltaAngle(yaw, tb.lastYaw)) > YawEpsilon;

            if (moved)
            {
                if (!tb.moving)
                {
                    // Anchor: where it rested until now.
                    tb.samples.Add(new EntitySample { t = tb.lastTime, pos = tb.lastPos, yaw = tb.lastYaw });
                    tb.moving = true;
                }
                tb.samples.Add(new EntitySample { t = _time, pos = pos, yaw = yaw });
            }
            else if (tb.moving)
            {
                tb.samples.Add(new EntitySample { t = _time, pos = pos, yaw = yaw }); // settle
                tb.moving = false;
            }

            tb.lastPos = pos;
            tb.lastYaw = yaw;
            tb.lastTime = _time;
        }
    }

    private void StartTracking(ReplayId rid)
    {
        _tracks.Add(new TrackBuilder
        {
            id = rid.Id,
            tf = rid.transform,
            lastPos = ToLocal(rid.transform.position),
            lastYaw = rid.transform.eulerAngles.y - RootYaw(),
            lastTime = _time,
        });
    }

    private void EmitEntitySample(TrackBuilder tb)
    {
        tb.samples.Add(new EntitySample
        {
            t = _time,
            pos = ToLocal(tb.tf.position),
            yaw = tb.tf.eulerAngles.y - RootYaw(),
        });
        tb.moving = false;
    }

    /// <summary>
    /// The recorded half's XZ footprint, from its tiles (spawn positions target
    /// tiles, so this is exactly the area runtime spawns can legitimately use).
    /// </summary>
    private void ComputeHalfBounds()
    {
        _hasHalfBounds = false;
        if (levelRoot == null) return;
        Tile[] tiles = levelRoot.GetComponentsInChildren<Tile>(true);
        if (tiles.Length == 0) return;

        _minX = float.MaxValue; _maxX = float.MinValue;
        _minZ = float.MaxValue; _maxZ = float.MinValue;
        foreach (Tile tile in tiles)
        {
            Vector3 p = ToLocal(tile.transform.position);
            if (p.x < _minX) _minX = p.x;
            if (p.x > _maxX) _maxX = p.x;
            if (p.z < _minZ) _minZ = p.z;
            if (p.z > _maxZ) _maxZ = p.z;
        }
        _minX -= HalfBoundsMargin; _maxX += HalfBoundsMargin;
        _minZ -= HalfBoundsMargin; _maxZ += HalfBoundsMargin;
        _hasHalfBounds = true;
    }

    private bool IsWithinRecordedHalf(Vector3 localPos) =>
        !_hasHalfBounds || (localPos.x >= _minX && localPos.x <= _maxX &&
                            localPos.z >= _minZ && localPos.z <= _maxZ);

    /// <summary>
    /// An obstacle finished initializing during recording. Scene obstacles carry a
    /// ReplayId already; anything without one is a runtime spawn (falling wave):
    /// it gets a runtime id in the recording scope plus a spawn event, and is
    /// tracked from here on (its fall is movement like any other).
    /// </summary>
    private void OnObstacleSpawned(Obstacle obstacle)
    {
        if (!_recording || _scope == null) return;
        if (ReplayId.Of(obstacle) != null) return; // authored scene obstacle

        // Spawns outside the recorded half (the other half's waves, stray debris)
        // are not part of this performance — recording them would materialize
        // them on the wrong side at playback.
        if (!IsWithinRecordedHalf(ToLocal(obstacle.transform.position)))
        {
            Debug.Log($"[StateReplayRecorder] Ignoring spawn of '{obstacle.name}' outside the recorded half.", obstacle);
            return;
        }

        int id = _scope.AllocateRuntimeId();
        ReplayId rid = obstacle.gameObject.AddComponent<ReplayId>();
        rid.AssignRuntime(_scope, id);

        Vector3 pos = ToLocal(obstacle.transform.position);
        float yaw = obstacle.transform.eulerAngles.y - RootYaw();
        _spawnEvents.Add(new ObstacleSpawnEvent
        {
            t = _time,
            entityId = id,
            obstacleType = obstacle.obstacleType,
            obstacleColor = obstacle.obstacleColor,
            pos = pos,
            yaw = yaw,
        });
        StartTracking(rid);
    }

    private void SamplePlayer()
    {
        _playerTrack.Add(new PlayerSample
        {
            t = _time,
            pos = ToLocal(target.transform.position),
            yaw = target.transform.eulerAngles.y - RootYaw(),
        });
    }

    private void CacheAnimParams()
    {
        _params.Clear();
        if (_animator == null)
        {
            Debug.LogWarning("[StateReplayRecorder] Target has no Animator; recording transform only.");
            return;
        }
        foreach (AnimatorControllerParameter p in _animator.parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Bool:
                    _params.Add(new ParamState { name = p.name, kind = AnimParamKind.Bool, last = _animator.GetBool(p.name) ? 1f : 0f });
                    break;
                case AnimatorControllerParameterType.Int:
                    _params.Add(new ParamState { name = p.name, kind = AnimParamKind.Int, last = _animator.GetInteger(p.name) });
                    break;
                case AnimatorControllerParameterType.Float:
                    _params.Add(new ParamState { name = p.name, kind = AnimParamKind.Float, last = _animator.GetFloat(p.name) });
                    break;
                case AnimatorControllerParameterType.Trigger:
                    Debug.LogWarning($"[StateReplayRecorder] Animator trigger '{p.name}' can't be recorded (triggers are consumed the frame they fire); it will be ignored.");
                    break;
            }
        }
    }

    /// <summary>Every parameter's value at t=0, so playback starts from the right animation state.</summary>
    private void EmitInitialAnimSnapshot()
    {
        foreach (ParamState p in _params)
            _animTrack.Add(new AnimParamEvent { t = 0f, param = p.name, kind = p.kind, value = p.last });
    }

    private void CaptureAnimChanges()
    {
        if (_animator == null) return;
        foreach (ParamState p in _params)
        {
            float current = p.kind switch
            {
                AnimParamKind.Bool => _animator.GetBool(p.name) ? 1f : 0f,
                AnimParamKind.Int => _animator.GetInteger(p.name),
                _ => _animator.GetFloat(p.name),
            };
            if (Mathf.Abs(current - p.last) < 0.0001f) continue;
            p.last = current;
            _animTrack.Add(new AnimParamEvent { t = _time, param = p.name, kind = p.kind, value = current });
        }
    }

    /// <summary>Copies the captured session into a replay asset. Called by the editor on save (and by the overlay tester).</summary>
    public void PopulateReplay(StateReplay replay)
    {
        replay.levelId = System.IO.Path.GetFileNameWithoutExtension(_scenePath);
        replay.duration = _time;
        replay.sampleInterval = _sampleInterval;
        replay.playerTrack = _playerTrack != null ? new List<PlayerSample>(_playerTrack) : new List<PlayerSample>();
        replay.playerAnimTrack = _animTrack != null ? new List<AnimParamEvent>(_animTrack) : new List<AnimParamEvent>();
        replay.events = _events != null ? new List<ReplayEvent>(_events) : new List<ReplayEvent>();
        replay.obstacleSpawnEvents = _spawnEvents != null ? new List<ObstacleSpawnEvent>(_spawnEvents) : new List<ObstacleSpawnEvent>();

        replay.entityTracks = new List<EntityTrack>();
        foreach (TrackBuilder tb in _tracks)
        {
            if (tb.samples.Count == 0) continue; // never moved — no track needed
            replay.entityTracks.Add(new EntityTrack
            {
                entityId = tb.id,
                samples = new List<EntitySample>(tb.samples),
            });
        }

        // Auto-set the scene reference from the recorded scene. FromScenePath
        // throws for scenes missing from Eflatun's GUID map (e.g. unsaved).
        if (!string.IsNullOrEmpty(_scenePath))
        {
            try { replay.scene = Eflatun.SceneReference.SceneReference.FromScenePath(_scenePath); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StateReplayRecorder] Could not auto-set the scene reference ({e.Message}). " +
                                 "Assign the replay's Scene field manually.");
            }
        }
    }

    private Player ResolveTarget()
    {
        if (GameManager.Instance != null)
        {
            Player local = GameManager.Instance.GetLocalPlayer();
            if (local != null) return local;
        }
        return FindObjectOfType<Player>();
    }

    private Vector3 ToLocal(Vector3 world) => levelRoot != null ? levelRoot.InverseTransformPoint(world) : world;
    private float RootYaw() => levelRoot != null ? levelRoot.eulerAngles.y : 0f;
}
