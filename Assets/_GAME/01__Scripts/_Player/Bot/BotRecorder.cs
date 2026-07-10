using System.Collections.Generic;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Records a live play session into the <see cref="BotReplay"/> action format so
/// authoring a bot = playing the level. It observes the target <see cref="Player"/>'s
/// resulting state (where it stops, when it pulls/jumps/attacks) rather than raw
/// input, which:
///   • is robust against new-Input-System Update/FixedUpdate timing quirks, and
///   • captures what actually happened, mapping cleanly onto the executor's
///     intent-level actions (<see cref="BotController"/>).
///
/// Positions are stored local to <see cref="levelRoot"/> (the same convention the
/// executor uses), and stats are captured at record time so playback timing
/// matches. Saving to an asset is handled by the editor (BotRecorderEditor).
///
/// Discrete actions (jump/hit/...) are recorded with the position at which they
/// fired and are replayed concurrently with movement at that position — so a jump
/// or hit performed mid-run is reproduced without stopping (e.g. jumping onto an
/// obstacle, hitting while falling).
/// </summary>
public class BotRecorder : MonoBehaviour
{
    [Tooltip("Player to record. If null, resolves to the local human player on Start Recording.")]
    public Player target;

    [Tooltip("Base transform positions are stored relative to — the level parent you're playing under. " +
             "Must match what BotController.levelRoot will be at playback (the opponent level root).")]
    public Transform levelRoot;

    [Tooltip("Begin recording automatically when play starts.")]
    public bool recordOnStart;

    [Tooltip("Treat a move direction shorter than this as 'stopped'.")]
    public float moveEpsilon = 0.01f;

    private PlayerMovement _movement;
    private PlayerController _controller;
    private Animator _anim;

    private bool _recording;
    private List<BotAction> _actions;

    // Captured at StartRecording.
    private Vector3 _startPos;
    private float _startYaw;
    private float _moveSpeed;
    private float _strength;
    private string _scenePath;

    // Edge-tracking state.
    private float _idleAccumulator;
    private Vector3 _lastMoveDir;
    private bool _wasPulling, _wasJumping, _wasHit, _wasHitDown, _wasSpecial;

    public bool IsRecording => _recording;
    public int ActionCount => _actions != null ? _actions.Count : 0;

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
            Debug.LogError("[BotRecorder] No target Player to record.");
            return;
        }

        _movement = target.GetComponent<PlayerMovement>();
        _controller = target.GetComponent<PlayerController>();
        _anim = target.GetComponent<Animator>();

        _actions = new List<BotAction>();
        _startPos = ToLocal(target.transform.position);
        _startYaw = target.transform.eulerAngles.y - RootYaw();
        _moveSpeed = target.StartingMoveSpeed;
        _strength = target.StartingStrenght;
        _scenePath = SceneManager.GetActiveScene().path;

        _idleAccumulator = 0f;
        _lastMoveDir = Vector3.zero;
        _wasPulling = _wasJumping = _wasHit = _wasHitDown = _wasSpecial = false;

        _recording = true;
        Debug.Log("[BotRecorder] Recording started.");
    }

    public void StopRecording()
    {
        if (!_recording) return;
        // Flush a trailing movement segment so the last waypoint is captured.
        if (_lastMoveDir != Vector3.zero) EmitMove();
        _recording = false;
        Debug.Log($"[BotRecorder] Recording stopped. {ActionCount} actions captured.");
    }

    private void Update()
    {
        if (!_recording || target == null) return;

        Vector3 moveDir = _movement != null ? _movement.CurrentMoveDirection : Vector3.zero;
        bool moving = moveDir.sqrMagnitude > moveEpsilon * moveEpsilon;
        bool pulling = _controller != null && _controller.isPulling;
        bool jumping = _movement != null && _movement.IsJumping;
        bool hit = _anim != null && _anim.GetBool("Hit");
        bool hitDown = _anim != null && _anim.GetBool("HitDown");
        bool special = _anim != null && _anim.GetBool("HitSpecial");

        // Accumulate idle time only while doing nothing — this becomes the
        // preDelay ("thinking pause") of the next action emitted.
        bool busy = moving || pulling || jumping || hit || hitDown || special;
        if (!busy) _idleAccumulator += Time.deltaTime;

        // --- Movement: emit a waypoint at each corner (direction change) and at
        //     each stop, giving a faithful cardinal polyline of the path.
        if (moving)
        {
            if (_lastMoveDir != Vector3.zero && !SameDir(moveDir, _lastMoveDir))
                EmitMove(); // corner
            _lastMoveDir = moveDir;
        }
        else if (_lastMoveDir != Vector3.zero)
        {
            EmitMove(); // stop
            _lastMoveDir = Vector3.zero;
        }

        // --- Pull: capture the player's end position on release (the value the
        //     executor closes the loop on).
        if (!pulling && _wasPulling) EmitPull();
        _wasPulling = pulling;

        // --- Discrete actions on their rising edge. Position the bot first if it
        //     was mid-run so the action happens at the right spot.
        if (jumping && !_wasJumping) EmitDiscrete(BotActionType.Jump);
        _wasJumping = jumping;
        if (hit && !_wasHit) EmitDiscrete(BotActionType.Hit);
        _wasHit = hit;
        if (hitDown && !_wasHitDown) EmitDiscrete(BotActionType.HitDown);
        _wasHitDown = hitDown;
        if (special && !_wasSpecial) EmitDiscrete(BotActionType.Special);
        _wasSpecial = special;
    }

    private void EmitMove()
    {
        _actions.Add(new BotAction(BotActionType.MoveToPosition)
        {
            targetPosition = ToLocal(target.transform.position),
            preDelay = _idleAccumulator,
        });
        _idleAccumulator = 0f;
    }

    private void EmitPull()
    {
        _actions.Add(new BotAction(BotActionType.Pull)
        {
            targetPosition = ToLocal(target.transform.position),
            preDelay = _idleAccumulator,
        });
        _idleAccumulator = 0f;
    }

    private void EmitDiscrete(BotActionType type)
    {
        // Record where the action fired (its anchor). The executor fires it by
        // position along the movement leg it falls within, concurrently with the
        // walk — so we do NOT insert a stop here. (preDelay is only used if this
        // ends up as a standing/dwell action with no movement around it.)
        _actions.Add(new BotAction(type)
        {
            targetPosition = ToLocal(target.transform.position),
            preDelay = _idleAccumulator,
        });
        _idleAccumulator = 0f;
    }

    /// <summary>Copies the captured session into a replay asset. Called by the editor on save.</summary>
    public void PopulateReplay(BotReplay replay)
    {
        replay.levelId = System.IO.Path.GetFileNameWithoutExtension(_scenePath);
        replay.startPosition = _startPos;
        replay.startYaw = _startYaw;
        replay.moveSpeed = _moveSpeed;
        replay.strength = _strength;
        replay.actions = _actions != null ? new List<BotAction>(_actions) : new List<BotAction>();

        // Auto-set the scene reference from the scene we recorded in. Wrapped
        // because FromScenePath throws if the scene isn't in Eflatun's GUID map
        // (e.g. an unsaved scene); the user can then assign it manually.
        if (!string.IsNullOrEmpty(_scenePath))
        {
            try { replay.scene = SceneReference.FromScenePath(_scenePath); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BotRecorder] Could not auto-set the scene reference ({e.Message}). " +
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

    private static bool SameDir(Vector3 a, Vector3 b)
    {
        if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f) return false;
        return Vector3.Dot(a.normalized, b.normalized) > 0.99f;
    }
}
