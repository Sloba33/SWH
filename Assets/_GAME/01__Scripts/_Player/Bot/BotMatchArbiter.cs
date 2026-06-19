using UnityEngine;

/// <summary>
/// Decides the outcome of a local bot match and routes it through the existing
/// multiplayer end-of-match flow (<see cref="LevelGoal.WinLevel"/> /
/// <see cref="LevelGoal.LoseLevel"/>, which set the MP trophy deltas and trigger
/// the MP win/lose screens via Settings).
///
/// Why this is needed: a real multiplayer match has one Settings/LevelGoal per
/// client and learns the opponent's result over the network. A local bot match
/// has a single shared Settings/LevelGoal and two players in one scene, so no
/// single player's own win/lose path can decide the match. This arbiter is that
/// single decision point.
///
/// Outcome mapping (from the local human's perspective):
///   • Human clears their goal first  → win  (handled by LevelGoal.RemoveObstacle
///     → WinLevel; we just observe gameWon so we don't also fire a loss).
///   • Bot clears its goal first      → lose (this class detects it — nothing
///     else decrements the bot's obstacle set locally).
///   • Human dies                     → lose (routed here from Player.Die).
///   • Bot dies                       → win  (routed here from Player.Die).
/// </summary>
public class BotMatchArbiter : MonoBehaviour
{
    public static BotMatchArbiter Instance { get; private set; }

    private Player _human;
    private LevelGoal _levelGoal;
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

    public void Initialize(Player human, LevelGoal levelGoal)
    {
        _human = human;
        _levelGoal = levelGoal;
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
            return;
        }

        // Don't evaluate a bot-cleared loss during the start grace — gives the
        // level a moment to finish initializing its obstacle sets.
        if (_elapsed < startGraceSeconds) return;

        int botRemaining = CountRemaining(_levelGoal.ObstaclesToDestroy_Opponent);
        if (botRemaining > 0) _armed = true;
        else if (_armed) ResolveLose("bot cleared its goal");
    }

    /// <summary>Called from <see cref="Player.Die"/> in a bot match.</summary>
    public void NotifyDeath(Player who)
    {
        if (_resolved) return;
        if (who == _human) ResolveLose("local human died");
        else ResolveWin("bot died");
    }

    private void ResolveWin(string reason)
    {
        if (_resolved || _levelGoal == null) return;
        _resolved = true;
        Debug.Log($"[BotMatchArbiter] Resolving WIN ({reason}).");
        StartCoroutine(_levelGoal.WinLevel(0.9f));
    }

    private void ResolveLose(string reason)
    {
        if (_resolved || _levelGoal == null) return;
        _resolved = true;
        Debug.Log($"[BotMatchArbiter] Resolving LOSE ({reason}).");
        StartCoroutine(_levelGoal.LoseLevel());
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
            // Destroyed obstacles read as Unity-null; pooled/deactivated ones are
            // non-null but disabled. Both mean "cleared".
            if (o != null && o.isActiveAndEnabled) count++;
        }
        return count;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
