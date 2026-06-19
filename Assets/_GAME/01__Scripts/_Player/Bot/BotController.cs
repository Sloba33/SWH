using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a <see cref="BotReplay"/> by feeding synthetic input into
/// <see cref="PlayerInputHandler"/> (BotControlled mode). It never touches
/// movement physics directly: it sets MoveInput / queues button presses exactly
/// like a human's controls would, so PlayerMovement, PlayerObstacleController and
/// PlayerAttack run unchanged. That is what makes the bot indistinguishable from
/// a real opponent in a multiplayer match.
///
/// Movement is closed-loop (drive toward a target world position and stop on
/// arrival) rather than timed input playback, so it self-corrects and never
/// drifts off the level over a long sequence.
///
/// Per-instance variation (start delay + per-action "thinking" jitter) lets one
/// recording produce several distinct-feeling opponents, so a small library of
/// replays doesn't feel repetitive.
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
    [Tooltip("Extra random pause added on top of each action's recorded preDelay.")]
    public float actionJitterMin = 0f;
    public float actionJitterMax = 0.25f;

    [Header("Movement tuning")]
    [Tooltip("How close (world units) counts as 'arrived' on an axis.")]
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

        foreach (BotAction action in replay.actions)
        {
            if (!_running) yield break;

            float pause = action.preDelay + Random.Range(actionJitterMin, actionJitterMax);
            if (pause > 0f)
            {
                _input.BotSetMove(Vector2.zero);
                yield return new WaitForSeconds(pause);
            }

            yield return ExecuteAction(action);
        }

        _input.BotSetMove(Vector2.zero);
        _running = false;
    }

    private IEnumerator ExecuteAction(BotAction action)
    {
        switch (action.type)
        {
            case BotActionType.MoveToPosition:
                yield return MoveTo(action.targetPosition);
                break;

            case BotActionType.Jump:
                _input.BotQueueJump();
                yield return new WaitForSeconds(0.15f);
                break;

            case BotActionType.Hit:
                _input.BotQueueHit();
                yield return new WaitForSeconds(0.3f);
                break;

            case BotActionType.HitDown:
                _input.BotQueueHitDown();
                yield return new WaitForSeconds(0.5f);
                break;

            case BotActionType.Special:
                _input.BotQueueSpecial();
                yield return new WaitForSeconds(0.6f);
                break;

            case BotActionType.Pull:
                yield return DoPull(action.targetPosition);
                break;

            case BotActionType.Wait:
                _input.BotSetMove(Vector2.zero);
                yield return new WaitForSeconds(action.duration);
                break;
        }
    }

    /// <summary>
    /// Walk to a position one cardinal axis at a time (X then Z), matching how
    /// PlayerInputHandler quantizes human input to a single axis. Everything is
    /// done in the level's local frame so the path follows the (possibly rotated)
    /// level grid; the cardinal local direction is converted to the world-space
    /// MoveInput that PlayerMovement consumes. Holding MoveInput drives the real
    /// velocity / push detection — we just pick direction and detect arrival.
    /// </summary>
    private IEnumerator MoveTo(Vector3 localTarget)
    {
        float elapsed = 0f;

        // Local X axis first.
        while (Mathf.Abs(localTarget.x - ToLocal(transform.position).x) > arriveThreshold)
        {
            float dir = Mathf.Sign(localTarget.x - ToLocal(transform.position).x);
            _input.BotSetMove(WorldMoveFromLocalDir(new Vector3(dir, 0f, 0f)));
            elapsed += Time.fixedDeltaTime;
            if (elapsed > moveTimeout) break;
            yield return new WaitForFixedUpdate();
        }

        // Then local Z axis.
        while (Mathf.Abs(localTarget.z - ToLocal(transform.position).z) > arriveThreshold)
        {
            float dir = Mathf.Sign(localTarget.z - ToLocal(transform.position).z);
            _input.BotSetMove(WorldMoveFromLocalDir(new Vector3(0f, 0f, dir)));
            elapsed += Time.fixedDeltaTime;
            if (elapsed > moveTimeout) break;
            yield return new WaitForFixedUpdate();
        }

        _input.BotSetMove(Vector2.zero);
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
    /// Latch + pull until the player reaches <paramref name="target"/>, then
    /// release. The bot is assumed to already be adjacent and facing the obstacle
    /// (the MoveToPosition that brought it here set its forward). One pull-press
    /// latches via PlayerController.StartPull; the obstacle is then dragged
    /// automatically by PlayerObstacleController for as long as the button is
    /// "held". Because the obstacle's travel-per-second changes with move speed,
    /// upgrades and weight, we close the loop on the player's end position rather
    /// than a fixed duration so the result is drift-free.
    ///
    /// We also bail the moment the obstacle vanishes: once the pull has actually
    /// engaged, a null pullObstacle means the sim destroyed or aborted it, so
    /// there's nothing left to pull toward the target.
    /// </summary>
    private IEnumerator DoPull(Vector3 localTarget)
    {
        _input.BotSetMove(Vector2.zero);
        _input.BotQueuePull();

        Vector3 worldTarget = ToWorld(localTarget);
        PlayerObstacleController obstacles = _playerController.playerObstacleController;
        bool engaged = false;
        float elapsed = 0f;

        while (true)
        {
            Vector3 planar = worldTarget - transform.position;
            planar.y = 0f;
            if (planar.magnitude <= pullArriveThreshold) break;

            // Watch the live pull target: once it has engaged, losing it means
            // the obstacle was destroyed or the pull was aborted by the sim.
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
