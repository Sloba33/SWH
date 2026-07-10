using UnityEngine;

/// <summary>
/// Decides the outcome of a local bot match and routes it through the existing
/// multiplayer end-of-match flow (<see cref="LevelGoal.WinLevel"/> /
/// <see cref="LevelGoal.LoseLevel"/>, which set the MP trophy deltas and trigger
/// the MP win/lose screens via Settings).
///
/// Why this is needed: a real multiplayer match has one Settings/LevelGoal per
/// client and learns the opponent's result over the network. A local bot match
/// has a single shared Settings/LevelGoal, a live human and a state-replay ghost
/// in one scene, so no single gameplay path can decide the match. This arbiter
/// is that single decision point; the first outcome stands and everything else
/// is latched out.
///
/// Outcome mapping (from the local human's perspective):
///   • Human clears their goal first → win. Handled by the live game
///     (LevelGoal.RemoveObstacle → WinLevel); we observe gameWon and latch.
///   • Bot clears its goal first → lose. The replay's destroy events remove the
///     entities in ObstaclesToDestroy_Opponent; we poll that count — nothing
///     else tracks the bot's goal locally.
///   • Human dies → lose. Routed here from Player.Die.
///   • Bot dies → win. The replay's PlayerDied event, surfaced by
///     StateReplayDriver.ReplayPlayerDied (the ghost's own Player component is
///     disabled and can never die).
///
/// On resolve the ghost driver is stopped so the bot freezes alongside the
/// obstacles (the MP end screens call FreezeObstacles themselves).
/// </summary>
public class BotMatchArbiter : MonoBehaviour
{
    public static BotMatchArbiter Instance { get; private set; }

    private Player _human;
    private LevelGoal _levelGoal;
    private StateReplayDriver _botDriver;
    private Settings _settings;

    // Arm the bot-clear check only once the bot's obstacle set has actually been
    // seen populated, so an empty/not-yet-initialized list can't read as an
    // instant loss at match start.
    private bool _armed;
    private bool _resolved;

    [Tooltip("Ignore the bot-cleared loss check for this long after the match starts, so " +
             "first-frame initialization can't read as an instant loss.")]
    public float startGraceSeconds = 1.0f;
    private float _elapsed;

    public void Initialize(Player human, LevelGoal levelGoal, StateReplayDriver botDriver)
    {
        _human = human;
        _levelGoal = levelGoal;
        _botDriver = botDriver;
        if (_botDriver != null)
            _botDriver.ReplayPlayerDied += NotifyBotDied;
        Instance = this;
    }

    private void Awake()
    {
        // Set early too, so Player.Die can find us even before Initialize runs.
        Instance = this;
    }

    private void Update()
    {
        if (_resolved || _levelGoal == null) return;
        _elapsed += Time.deltaTime;

        // If the human already won (or any path already ended the match), latch
        // resolved so we never fire a contradicting loss.
        if (IsGameOver())
        {
            _resolved = true;
            StopBot();
            return;
        }

        // Don't evaluate a bot-cleared loss during the start grace — gives the
        // level a moment to finish initializing its obstacle sets.
        if (_elapsed < startGraceSeconds) return;

        int botRemaining = CountRemaining(_levelGoal.ObstaclesToDestroy_Opponent);
        if (botRemaining > 0) _armed = true;
        else if (_armed) ResolveLose("bot cleared its goal");
    }

    /// <summary>Called from <see cref="Player.Die"/> in a bot match (only the live human can reach it).</summary>
    public void NotifyDeath(Player who)
    {
        if (_resolved) return;
        if (who == _human) ResolveLose("local human died");
    }

    /// <summary>The replay reached its recorded death — the bot is dead, the human wins.</summary>
    public void NotifyBotDied()
    {
        ResolveWin("bot died in replay");
    }

    private void ResolveWin(string reason)
    {
        if (_resolved || _levelGoal == null) return;
        _resolved = true;
        Debug.Log($"[BotMatchArbiter] Resolving WIN ({reason}).");
        StopBot();
        StartCoroutine(_levelGoal.WinLevel(0.9f));
    }

    private void ResolveLose(string reason)
    {
        if (_resolved || _levelGoal == null) return;
        _resolved = true;
        Debug.Log($"[BotMatchArbiter] Resolving LOSE ({reason}).");
        StopBot();
        StartCoroutine(_levelGoal.LoseLevel());
    }

    /// <summary>
    /// Freezes the ghost where it stands (the end screens freeze the obstacles).
    /// Stopping only halts replay time — the Animator stays enabled, so an
    /// already-triggered death animation still plays out.
    /// </summary>
    private void StopBot()
    {
        if (_botDriver == null) return;
        _botDriver.ReplayPlayerDied -= NotifyBotDied;
        _botDriver.Stop();
    }

    private bool IsGameOver()
    {
        if (_settings == null) _settings = FindObjectOfType<Settings>();
        return _settings != null && (_settings.gameWon || _settings.gameLost);
    }

    private static int CountRemaining(System.Collections.Generic.List<Obstacle> obstacles)
    {
        if (obstacles == null) return 0;
        int count = 0;
        foreach (Obstacle o in obstacles)
        {
            // Replay-destroyed obstacles read as Unity-null; anything inactive is
            // also treated as cleared.
            if (o != null && o.isActiveAndEnabled) count++;
        }
        return count;
    }

    private void OnDestroy()
    {
        if (_botDriver != null) _botDriver.ReplayPlayerDied -= NotifyBotDied;
        if (Instance == this) Instance = null;
    }
}
