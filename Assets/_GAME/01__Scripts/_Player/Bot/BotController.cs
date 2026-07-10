using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a <see cref="BotReplay"/> by feeding synthetic input into
/// <see cref="PlayerInputHandler"/> (BotControlled mode). It never touches
/// movement physics directly: it sets MoveInput / queues button presses exactly
/// like a human's controls would, so PlayerMovement, PlayerObstacleController and
/// PlayerAttack run unchanged — that is what makes the bot indistinguishable from
/// a real opponent.
///
/// Concurrent model: movement is the continuous backbone (a path of waypoints
/// walked without stopping), and discrete actions (jump/hit/hit-down/special)
/// are <i>overlays</i> anchored to the position along the current movement leg
/// where they were recorded. They are queued at that point without interrupting
/// movement, so a hit/jump happens <i>while</i> moving/jumping/falling and a jump
/// can carry the bot onto an obstacle. Pull is a stateful leg that drives the bot
/// to a recorded end position (works airborne too).
///
/// Everything is position-anchored (not timed), so it self-corrects and never
/// drifts against the level's timed/falling obstacles.
///
/// Per-instance variation (start delay + per-pause "thinking" jitter) lets one
/// recording produce several distinct-feeling opponents.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class BotController : MonoBehaviour
{
    [Header("What to play")]
    public BotReplay replay;
    [Tooltip("Start playing automatically on Start. Turn off if a match controller will call Play() after spawning/positioning the bot.")]
    public bool autoPlay = true;
    [Tooltip("Base transform the replay's positions are relative to (typically the opponent level's parent). " +
             "All replay positions/yaw are treated as local to this; leave null to use world space.")]
    public Transform levelRoot;

    [Header("Variation (one recording → many opponents)")]
    [Tooltip("Random delay before the bot makes its first move.")]
    public float startDelayMin = 0.3f;
    public float startDelayMax = 1.0f;
    [Tooltip("Extra random pause added on top of each recorded pause.")]
    public float actionJitterMin = 0f;
    public float actionJitterMax = 0.25f;

    [Header("Movement tuning")]
    [Tooltip("Planar (XZ) distance to a waypoint that counts as 'arrived' on an axis.")]
    public float arriveThreshold = 0.06f;
    [Tooltip("Safety cap so a blocked move can never hang the sequence forever.")]
    public float moveTimeout = 8f;

    [Header("Pull tuning")]
    [Tooltip("Planar (XZ) distance to the pull end position that counts as 'done'. Looser than arriveThreshold because the pull snaps the player to the obstacle each tick.")]
    public float pullArriveThreshold = 0.12f;
    [Tooltip("Safety cap so a pull that never reaches its target can't hang the sequence.")]
    public float pullTimeout = 8f;

    private PlayerController _playerController;
    private PlayerInputHandler _input;
    private Player _player;
    private bool _running;

    // A leg is one stretch of the path: a continuous walk to a waypoint, a pull,
    // or an in-place dwell. Discrete actions recorded during a leg ride along as
    // overlays and fire by position, concurrently with the movement.
    private enum LegKind { Move, Pull, Dwell }

    private class Leg
    {
        public LegKind kind;
        public Vector3 target;   // local; Move/Pull
        public float preDelay;   // pause before this leg starts
        public float dwellWait;  // extra idle for a Wait action (Dwell only)
        public readonly List<BotAction> overlays = new List<BotAction>();
    }

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _input = GetComponent<PlayerInputHandler>();
        _player = GetComponent<Player>();

        // Flag as AI + bot-controlled as early as possible: PlayerController and
        // PlayerObstacleController branch on .AI to skip UI/PlayerControls hooks
        // a bot doesn't have, and PlayerInputHandler must ignore real devices.
        _playerController.AI = true;
        _input.BotControlled = true;

        SuppressPresentation();
    }

    /// <summary>
    /// A bot is an opponent, never the local viewpoint, so it must not own a
    /// camera/audio listener — the human player has those. This mirrors how a
    /// multiplayer *remote* player tears down its camera in PlayerController.Start
    /// (Destroy of the camera GameObjects), except we keep movement/animation
    /// enabled since the bot is simulated locally. Destroying (not just disabling)
    /// matches MP and is safe: PlayerController.Start null-checks followCamera
    /// before using it.
    /// </summary>
    private void SuppressPresentation()
    {
        if (_playerController.playerCamera != null)
            Destroy(_playerController.playerCamera.gameObject);
        if (_playerController.followCamera != null)
            Destroy(_playerController.followCamera.gameObject);
    }

    private void Start()
    {
        if (autoPlay) Play();
    }

    /// <summary>Position/orient the bot per the replay and begin executing it.</summary>
    public void Play()
    {
        if (_running) return;
        if (replay == null)
        {
            Debug.LogWarning("[BotController] No replay assigned; nothing to play.");
            return;
        }
        _running = true;
        ApplyReplayStats();
        StartCoroutine(RunSequence());
    }

    /// <summary>
    /// Re-applies the bot's record-time stats so playback timing matches the
    /// recording — without this a faster/slower or stronger/weaker character
    /// would drift relative to the level's timed/falling obstacles. We set both
    /// the Starting* values (which Player.Start derives MoveSpeed/Strength from,
    /// if it runs after this) and the live values (if Player.Start already ran),
    /// so the result is correct regardless of Start ordering.
    /// </summary>
    private void ApplyReplayStats()
    {
        if (replay == null || _player == null) return;
        _player.StartingMoveSpeed = replay.moveSpeed;
        _player.MoveSpeed = replay.moveSpeed;
        _player.StartingStrenght = replay.strength;
        _player.Strength = replay.strength;
    }

    public void Stop()
    {
        _running = false;
        StopAllCoroutines();
        _input.BotSetMove(Vector2.zero);
    }

    private IEnumerator RunSequence()
    {
        // Place at the recorded start state (local to levelRoot) so the waypoints
        // line up regardless of where/how the level is positioned.
        transform.position = ToWorld(replay.startPosition);
        float rootYaw = levelRoot != null ? levelRoot.eulerAngles.y : 0f;
        transform.rotation = Quaternion.Euler(0f, rootYaw + replay.startYaw, 0f);

        yield return new WaitForSeconds(Random.Range(startDelayMin, startDelayMax));

        List<Leg> legs = BuildLegs(replay.actions);
        Vector3 prevLocal = replay.startPosition;

        foreach (Leg leg in legs)
        {
            if (!_running) yield break;

            float pause = leg.preDelay + Random.Range(actionJitterMin, actionJitterMax);
            if (pause > 0f)
            {
                _input.BotSetMove(Vector2.zero);
                yield return new WaitForSeconds(pause);
            }

            switch (leg.kind)
            {
                case LegKind.Move:
                    yield return RunMoveLeg(prevLocal, leg.target, leg.overlays);
                    prevLocal = leg.target;
                    break;
                case LegKind.Pull:
                    FireOverlays(leg.overlays); // pull is exclusive; fire any overlays at its start
                    yield return RunPull(leg.target);
                    prevLocal = leg.target;
                    break;
                case LegKind.Dwell:
                    yield return RunDwell(leg);
                    break;
            }
        }

        _input.BotSetMove(Vector2.zero);
        _running = false;
    }

    /// <summary>
    /// Splits the flat recorded action list into legs. Discrete actions accumulate
    /// as overlays and attach to the next Move/Pull leg (they occurred during the
    /// travel toward that target). A Wait, or trailing discretes with no following
    /// movement, become a Dwell leg.
    /// </summary>
    private List<Leg> BuildLegs(List<BotAction> actions)
    {
        var legs = new List<Leg>();
        var pending = new List<BotAction>();

        foreach (BotAction a in actions)
        {
            switch (a.type)
            {
                case BotActionType.MoveToPosition:
                {
                    var leg = new Leg { kind = LegKind.Move, target = a.targetPosition, preDelay = a.preDelay };
                    leg.overlays.AddRange(pending);
                    pending.Clear();
                    legs.Add(leg);
                    break;
                }
                case BotActionType.Pull:
                {
                    var leg = new Leg { kind = LegKind.Pull, target = a.targetPosition, preDelay = a.preDelay };
                    leg.overlays.AddRange(pending);
                    pending.Clear();
                    legs.Add(leg);
                    break;
                }
                case BotActionType.Wait:
                {
                    var leg = new Leg { kind = LegKind.Dwell, preDelay = a.preDelay, dwellWait = a.duration };
                    leg.overlays.AddRange(pending);
                    pending.Clear();
                    legs.Add(leg);
                    break;
                }
                default: // discrete overlay (Jump/Hit/HitDown/Special)
                    pending.Add(a);
                    break;
            }
        }

        if (pending.Count > 0)
        {
            var leg = new Leg { kind = LegKind.Dwell };
            leg.overlays.AddRange(pending);
            legs.Add(leg);
        }

        return legs;
    }

    /// <summary>
    /// Walks from <paramref name="fromLocal"/> to <paramref name="toLocal"/> one
    /// cardinal axis at a time (X then Z), matching how PlayerInputHandler quantizes
    /// human input. Movement is in the level's local frame so the path follows the
    /// (possibly rotated) grid. Overlay actions fire by their fraction along this
    /// leg, queued without stopping the walk — so they overlap movement. Arrival is
    /// planar (ignores Y) so a jump that lands the bot on a raised obstacle still
    /// counts as reaching the waypoint.
    /// </summary>
    private IEnumerator RunMoveLeg(Vector3 fromLocal, Vector3 toLocal, List<BotAction> overlays)
    {
        // Pre-compute each overlay's fraction along the leg and sort ascending.
        Vector3 seg = toLocal - fromLocal; seg.y = 0f;
        float segLenSq = seg.x * seg.x + seg.z * seg.z;

        var fires = new List<(float frac, BotAction action)>(overlays.Count);
        foreach (BotAction a in overlays)
            fires.Add((LegFraction(a.targetPosition, fromLocal, seg, segLenSq), a));
        fires.Sort((x, y) => x.frac.CompareTo(y.frac));

        int next = 0;
        float elapsed = 0f;

        while (_running)
        {
            Vector3 curLocal = ToLocal(transform.position);
            float f = LegFraction(curLocal, fromLocal, seg, segLenSq);

            // Fire any overlays we've reached, in order — concurrent with movement.
            while (next < fires.Count && fires[next].frac <= f)
            {
                FireDiscrete(fires[next].action);
                next++;
            }

            bool reachedX = Mathf.Abs(toLocal.x - curLocal.x) <= arriveThreshold;
            bool reachedZ = Mathf.Abs(toLocal.z - curLocal.z) <= arriveThreshold;
            if (reachedX && reachedZ) break;

            Vector3 dirLocal = !reachedX
                ? new Vector3(Mathf.Sign(toLocal.x - curLocal.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(toLocal.z - curLocal.z));
            _input.BotSetMove(WorldMoveFromLocalDir(dirLocal));

            elapsed += Time.fixedDeltaTime;
            if (elapsed > moveTimeout) break;

            yield return new WaitForFixedUpdate();
        }

        // Fire any overlays not reached by position (e.g. anchored at the very end).
        while (next < fires.Count) { FireDiscrete(fires[next].action); next++; }

        // Deliberately do NOT zero MoveInput here: if the next leg is another move
        // it redirects immediately (continuous motion through waypoints); pauses
        // and the end of the sequence zero it in RunSequence.
    }

    /// <summary>Projection of a local point onto the leg, as a 0..1 fraction (planar).</summary>
    private static float LegFraction(Vector3 pointLocal, Vector3 fromLocal, Vector3 seg, float segLenSq)
    {
        if (segLenSq <= 1e-6f) return 1f;
        float dot = (pointLocal.x - fromLocal.x) * seg.x + (pointLocal.z - fromLocal.z) * seg.z;
        return Mathf.Clamp01(dot / segLenSq);
    }

    private void FireDiscrete(BotAction action)
    {
        switch (action.type)
        {
            case BotActionType.Jump: _input.BotQueueJump(); break;
            case BotActionType.Hit: _input.BotQueueHit(); break;
            case BotActionType.HitDown: _input.BotQueueHitDown(); break;
            case BotActionType.Special: _input.BotQueueSpecial(); break;
        }
    }

    private void FireOverlays(List<BotAction> overlays)
    {
        foreach (BotAction a in overlays) FireDiscrete(a);
    }

    /// <summary>An in-place segment: spaced discrete actions and/or a Wait.</summary>
    private IEnumerator RunDwell(Leg leg)
    {
        _input.BotSetMove(Vector2.zero);
        foreach (BotAction a in leg.overlays)
        {
            float pause = a.preDelay + Random.Range(actionJitterMin, actionJitterMax);
            if (pause > 0f) yield return new WaitForSeconds(pause);
            FireDiscrete(a);
            yield return new WaitForFixedUpdate();
        }
        if (leg.dwellWait > 0f) yield return new WaitForSeconds(leg.dwellWait);
    }

    // --- Level-relative helpers ---------------------------------------------
    // Replay positions are stored local to levelRoot so a level can be moved or
    // rotated wholesale and the replay still lines up. With no levelRoot these
    // are identity (world space), preserving the original behavior.

    private Vector3 ToWorld(Vector3 local) => levelRoot != null ? levelRoot.TransformPoint(local) : local;
    private Vector3 ToLocal(Vector3 world) => levelRoot != null ? levelRoot.InverseTransformPoint(world) : world;

    /// <summary>
    /// Converts a local cardinal direction into the world-space MoveInput vector
    /// (x,z) PlayerMovement expects, flattened to the ground plane.
    /// </summary>
    private Vector2 WorldMoveFromLocalDir(Vector3 localDir)
    {
        Vector3 w = levelRoot != null ? levelRoot.TransformDirection(localDir) : localDir;
        w.y = 0f;
        if (w.sqrMagnitude > 1e-6f) w.Normalize();
        return new Vector2(w.x, w.z);
    }

    /// <summary>
    /// Latch + pull until the player reaches <paramref name="localTarget"/>, then
    /// release. The bot is assumed to already be adjacent and facing the obstacle
    /// (the move leg that brought it here set its forward). One pull-press latches
    /// via PlayerController.StartPull; the obstacle is then dragged automatically
    /// for as long as the button is "held". We close the loop on the player's end
    /// position (not a fixed duration) so it can't drift when move speed varies,
    /// and bail the moment the obstacle vanishes (destroyed / aborted by the sim).
    /// Works airborne — the pull mechanic handles falling obstacles.
    /// </summary>
    private IEnumerator RunPull(Vector3 localTarget)
    {
        _input.BotSetMove(Vector2.zero);
        _input.BotQueuePull();

        Vector3 worldTarget = ToWorld(localTarget);
        PlayerObstacleController obstacles = _playerController.playerObstacleController;
        bool engaged = false;
        float elapsed = 0f;

        while (_running)
        {
            Vector3 planar = worldTarget - transform.position;
            planar.y = 0f;
            if (planar.magnitude <= pullArriveThreshold) break;

            if (obstacles != null && obstacles.pullObstacle != null) engaged = true;
            else if (engaged) break;

            elapsed += Time.fixedDeltaTime;
            if (elapsed > pullTimeout) break;

            yield return new WaitForFixedUpdate();
        }

        _input.BotQueuePullReleased();
        yield return new WaitForSeconds(0.1f);
    }
}
