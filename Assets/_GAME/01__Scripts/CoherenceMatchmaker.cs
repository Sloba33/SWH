using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Coherence.Cloud;
using Coherence.Runtime;

public static class CoherenceMatchmaker
{
    private const int MatchSize = 2;
    private static readonly TimeSpan DefaultBotFallbackTimeout = TimeSpan.FromSeconds(15);
    // Hard cap on the post-"opponent found" phase (starting the session, waiting
    // for the room-data push). Past the WaitingForOpponent state the Cancel
    // button is disabled, so any unbounded await here would strand the player on
    // "Game starting..." with no way out — a lost server push did exactly that.
    private static readonly TimeSpan GameStartTimeout = TimeSpan.FromSeconds(30);
    // Once the lobby is full (or we've joined one), the play-session push should
    // arrive within seconds. A longer silence means an orphaned/broken session —
    // e.g. we joined a lobby whose owner is gone and will never start the game.
    private static readonly TimeSpan RoomDataTimeout = TimeSpan.FromSeconds(15);

    public enum MatchmakingState
    {
        Idle,
        WaitingForOpponent,
        OpponentFound,
        GameStarting,
        Completed,
        BotFallback,
        Cancelled,
        Failed,
    }

    public readonly struct MatchResult
    {
        public LobbySession Lobby { get; }
        public RoomData Room { get; }
        public bool IsHost { get; }
        public bool IsBotFallback { get; }
        public string BotLevelScene { get; }
        /// <summary>
        /// The scene the multiplayer match should load. Resolved from the lobby's "map"
        /// attribute, so it is the authoritative source even when the caller passed an
        /// empty <c>mapName</c> (any-map matchmaking). Empty for bot-fallback results.
        /// </summary>
        public string MapName { get; }

        public MatchResult(LobbySession lobby, RoomData room, bool isHost, string mapName)
            : this(lobby, room, isHost, isBotFallback: false, botLevelScene: null, mapName: mapName) { }

        private MatchResult(LobbySession lobby, RoomData room, bool isHost, bool isBotFallback, string botLevelScene, string mapName)
        {
            Lobby = lobby;
            Room = room;
            IsHost = isHost;
            IsBotFallback = isBotFallback;
            BotLevelScene = botLevelScene;
            MapName = mapName;
        }

        internal static MatchResult ForBotFallback(string sceneName)
            => new(lobby: null, room: default, isHost: false, isBotFallback: true, botLevelScene: sceneName, mapName: null);
    }

    /// <summary>
    /// Current matchmaking state. The UI in the waiting scene binds to <see cref="StateChanged"/>
    /// to update its label and disable the cancel button once a match is locked in.
    /// </summary>
    public static MatchmakingState State { get; private set; } = MatchmakingState.Idle;

    /// <summary>Fires on the calling thread (Unity main thread for in-game callers) whenever <see cref="State"/> changes.</summary>
    public static event Action<MatchmakingState> StateChanged;

    /// <summary>Cancel is only meaningful while still searching — once an opponent is found the game starts immediately.</summary>
    public static bool CanCancel => State == MatchmakingState.WaitingForOpponent;

    /// <summary>
    /// Last result produced by <see cref="FindMatchAsync"/>. Persists across the scene
    /// load so the multiplayer GameManager can read the room without keeping a reference
    /// to the matchmaking scene. Cleared by <see cref="ClearLatestMatch"/>.
    /// </summary>
    public static MatchResult LatestMatch { get; private set; }

    private static CancellationTokenSource _activeCts;

    /// <summary>
    /// Cancels the in-flight matchmaking call (if any). Returns true if cancellation
    /// was requested. No-op once we have moved past the cancellable window
    /// (see <see cref="CanCancel"/>) or if no matchmaking is in progress.
    /// </summary>
    public static bool TryCancel()
    {
        if (!CanCancel)
        {
            return false;
        }
        var cts = _activeCts;
        if (cts == null)
        {
            return false;
        }
        try
        {
            cts.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    public static void ClearLatestMatch()
    {
        LatestMatch = default;
    }

    // A lobby we still owe a leave from the previous match. Kept until the leave
    // is CONFIRMED: a fire-and-forget leave losing the race against the scene
    // load (or failing silently) left both clients members of the old lobby, and
    // FindOrCreateLobbyAsync then handed that stale lobby straight back — the
    // root of the cross-scene room contamination.
    private static LobbySession _lobbyPendingLeave;

    // Lobbies that already failed us this app run (joined, then no play-session
    // push ever came — orphaned shells). Never join them again; without this a
    // single lingering orphan bricked every subsequent matchmaking attempt with
    // the same 15s room-data timeout until the server culled it.
    private static readonly HashSet<string> _deadLobbyIds = new HashSet<string>();

    /// <summary>
    /// Leave the current match's lobby without blocking a scene transition. The
    /// lobby is remembered until the leave is confirmed, and the next
    /// <see cref="FindMatchAsync"/> finishes the job (awaited) before searching.
    /// </summary>
    public static void LeaveCurrentLobbyInBackground()
    {
        LobbySession lobby = LatestMatch.Lobby ?? _lobbyPendingLeave;
        ClearLatestMatch();
        if (lobby == null) return;
        _lobbyPendingLeave = lobby;
        _ = LeaveAndForgetAsync(lobby);
    }

    private static async Task LeaveAndForgetAsync(LobbySession lobby)
    {
        bool left = await AbandonLobbyAsync(lobby);
        if (left && ReferenceEquals(_lobbyPendingLeave, lobby))
            _lobbyPendingLeave = null;
    }

    /// <summary>
    /// Finds or creates a 1v1 lobby, waits until the game session starts,
    /// and returns the room to connect to (call <c>bridge.JoinRoom(result.Room)</c>).
    /// <para>
    /// Pass an empty/null <paramref name="mapName"/> for any-map matchmaking: the search
    /// drops its map filter so any open lobby qualifies, and if we end up creating the
    /// lobby instead a random scene from <paramref name="multiplayerLevels"/> is chosen
    /// for it. The caller should load <see cref="MatchResult.MapName"/> (the resolved
    /// map) rather than the input <paramref name="mapName"/>.
    /// </para>
    /// <para>
    /// When <paramref name="fallbackToBotEnabled"/> is true and <paramref name="botLevels"/>
    /// is non-empty, the host falls back to a randomly picked bot scene if no opponent
    /// joins within <paramref name="botFallbackTimeout"/> (default 15s). In that case the
    /// returned <see cref="MatchResult.IsBotFallback"/> is true and the caller should load
    /// <see cref="MatchResult.BotLevelScene"/> as a single-player level instead of joining a room.
    /// </para>
    /// <para>
    /// <c>onProgress</c> is an optional callback that receives a human-readable
    /// status string each time the matchmaker advances to the next step.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when input parameters are invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token or <see cref="TryCancel"/> is signalled.</exception>
    /// <exception cref="MatchmakingException">Thrown for any matchmaking failure (login, lobby, room).</exception>
    public static async Task<MatchResult> FindMatchAsync(
        string mapName,
        int skillLevel,
        int minOpponentSkill,
        int maxOpponentSkill,
        string region = null,
        IReadOnlyList<string> multiplayerLevels = null,
        IReadOnlyList<string> botLevels = null,
        bool fallbackToBotEnabled = false,
        TimeSpan? botFallbackTimeout = null,
        Action<string> onProgress = null,
        CancellationToken cancellationToken = default)
    {
        bool anyMap = string.IsNullOrWhiteSpace(mapName);
        if (anyMap && (multiplayerLevels == null || multiplayerLevels.Count == 0))
        {
            throw new ArgumentException(
                "Empty mapName requires a non-empty multiplayerLevels list — one of those scenes is " +
                "picked at random when we have to create the lobby.",
                nameof(multiplayerLevels));
        }
        if (minOpponentSkill > maxOpponentSkill)
        {
            throw new ArgumentException(
                $"minOpponentSkill ({minOpponentSkill}) must be <= maxOpponentSkill ({maxOpponentSkill}).",
                nameof(minOpponentSkill));
        }

        // Link the external token with an internal CTS so TryCancel() can trip the same
        // wires as an externally-supplied cancellation. Stored statically because there is
        // only ever one matchmaking session in flight in this game.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = linkedCts.Token;
        var previousCts = Interlocked.Exchange(ref _activeCts, linkedCts);

        SetState(MatchmakingState.WaitingForOpponent);
        Report(onProgress, "Logging in to coherence Cloud...");

        try
        {
            var result = await FindMatchInternalAsync(
                mapName, skillLevel, minOpponentSkill, maxOpponentSkill, region,
                multiplayerLevels, botLevels, fallbackToBotEnabled,
                botFallbackTimeout ?? DefaultBotFallbackTimeout,
                onProgress, token);

            LatestMatch = result;
            SetState(result.IsBotFallback ? MatchmakingState.BotFallback : MatchmakingState.Completed);
            return result;
        }
        catch (OperationCanceledException)
        {
            SetState(MatchmakingState.Cancelled);
            throw;
        }
        catch
        {
            SetState(MatchmakingState.Failed);
            throw;
        }
        finally
        {
            Interlocked.CompareExchange(ref _activeCts, null, linkedCts);
            // If a stale session was still recorded when we started, clean it up so a
            // subsequent TryCancel doesn't accidentally hit the disposed one.
            if (previousCts != null && !ReferenceEquals(previousCts, linkedCts))
            {
                Interlocked.CompareExchange(ref _activeCts, null, previousCts);
            }
        }
    }

    private static async Task<MatchResult> FindMatchInternalAsync(
        string mapName,
        int skillLevel,
        int minOpponentSkill,
        int maxOpponentSkill,
        string region,
        IReadOnlyList<string> multiplayerLevels,
        IReadOnlyList<string> botLevels,
        bool fallbackToBotEnabled,
        TimeSpan botFallbackTimeout,
        Action<string> onProgress,
        CancellationToken cancellationToken)
    {
        bool anyMap = string.IsNullOrWhiteSpace(mapName);
        // The map we will create a lobby for IF we end up as host. For any-map matchmaking
        // this is picked at random from the multiplayer pool so the resulting lobby still
        // advertises a concrete scene that joiners can load.
        string createMapName = anyMap
            ? multiplayerLevels[UnityEngine.Random.Range(0, multiplayerLevels.Count)]
            : mapName;

        PlayerAccount playerAccount;
        try
        {
            var loginOperation = await CoherenceCloud.LoginAsGuest(cancellationToken);

            if (loginOperation.HasFailed)
            {
                var error = loginOperation.Error;
                throw new MatchmakingException($"Login failed: {error.Type} - {error.Message}");
            }

            playerAccount = loginOperation.Result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MatchmakingException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new MatchmakingException("Login to coherence Cloud failed.", e);
        }

        if (playerAccount is null || !playerAccount.IsLoggedIn)
        {
            throw new MatchmakingException("Player account is not logged in after login completed.");
        }

        var lobbyService = playerAccount.Services?.Rooms?.LobbyService
            ?? throw new MatchmakingException("LobbiesService is not available on the player account.");

        // Defense against stale lobby reuse: the post-match leave is backgrounded
        // and can lose the race against the scene load (or fail silently). A
        // lingering membership lets FindOrCreateLobbyAsync hand the PREVIOUS
        // match's lobby straight back — instantly "full", with the old match's
        // map attribute — which is how two clients ended up in one room with
        // different scenes loaded. Finish any owed leave here, awaited.
        LobbySession lingering = _lobbyPendingLeave ?? LatestMatch.Lobby;
        if (lingering != null)
        {
            Report(onProgress, "Cleaning up previous match session...");
            if (await AbandonLobbyAsync(lingering))
                _lobbyPendingLeave = null;
            ClearLatestMatch();
        }

        Report(onProgress, "Searching for an available lobby...");

        // Lobby attribute layout:
        //   s1 = map name
        //   n1 = host's own skill
        //   n2 = host's min acceptable opponent skill
        //   n3 = host's max acceptable opponent skill
        // Closed and unlisted lobbies are excluded from /match by coherence implicitly,
        // so a lobby that has already started a game session is no longer matchable.
        // When matchmaking with any-map we drop the s1 constraint so the search matches
        // lobbies on any map; the lobby we *create* still advertises a concrete map.
        var filter = new LobbyFilter().WithAnd();
        if (!anyMap)
        {
            filter = filter.WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, mapName);
        }
        filter = filter
            .WithIntAttribute(FilterOperator.GreaterOrEqualThan, IntAttributeIndex.n1, minOpponentSkill)
            .WithIntAttribute(FilterOperator.LessOrEqualThan,    IntAttributeIndex.n1, maxOpponentSkill)
            .WithIntAttribute(FilterOperator.LessOrEqualThan,    IntAttributeIndex.n2, skillLevel)
            .WithIntAttribute(FilterOperator.GreaterOrEqualThan, IntAttributeIndex.n3, skillLevel)
            .WithAvailableSlots(FilterOperator.GreaterThan, 0);

        var findOptions = new FindLobbyOptions
        {
            Limit = 20,
            LobbyFilters = new List<LobbyFilter> { filter },
            Sort = new Dictionary<SortOptions, bool> { { SortOptions.numPlayers, true } },
        };

        var createOptions = new CreateLobbyOptions
        {
            MaxPlayers = MatchSize,
            Region = region,
            LobbyAttributes = new List<CloudAttribute>
            {
                new("map",       createMapName,    StringAttributeIndex.s1, StringAggregator.None, isPublic: true),
                new("skill",     skillLevel,       IntAttributeIndex.n1,    IntAggregator.None,    isPublic: true),
                new("min_skill", minOpponentSkill, IntAttributeIndex.n2,    IntAggregator.None,    isPublic: true),
                new("max_skill", maxOpponentSkill, IntAttributeIndex.n3,    IntAggregator.None,    isPublic: true),
            },
        };

        LobbySession lobbySession = null;
        for (int attempt = 0; attempt < 3 && lobbySession == null; attempt++)
        {
            // Last attempt goes create-only: if the search keeps returning
            // unusable lobbies (orphaned shells that /match still lists), stop
            // fishing in the poisoned pool and host a fresh lobby instead —
            // worst case that resolves via the bot fallback, never a dead end.
            bool createOnly = attempt == 2;
            try
            {
                lobbySession = createOnly
                    ? await lobbyService.CreateLobbyAsync(createOptions, cancellationToken)
                    : await lobbyService.FindOrCreateLobbyAsync(findOptions, createOptions, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RequestException re)
            {
                throw new MatchmakingException($"Failed to find or create lobby: {re.ErrorCode} - {re.Message}", re);
            }
            catch (Exception e)
            {
                throw new MatchmakingException("Failed to find or create lobby.", e);
            }

            // Last line of defense against stale/orphaned lobbies — never build a
            // match on one. Leaving an orphan as its last member also empties it
            // so the server can cull it instead of it re-matching forever.
            if (IsUnusableLobby(lobbySession, createMapName, out string staleReason))
            {
                UnityEngine.Debug.LogWarning(
                    $"[Matchmaker] Unusable lobby '{lobbySession.LobbyData.Id}' ({staleReason}, " +
                    $"players: {lobbySession.LobbyData.Players.Count}) — abandoning it and retrying.");
                await AbandonLobbyAsync(lobbySession);
                lobbySession = null;
            }
        }
        if (lobbySession == null)
        {
            throw new MatchmakingException("Matchmaking kept receiving unusable (stale/orphaned) lobbies; aborting this attempt.");
        }

        // If we fail or get cancelled after this point, leave the lobby on the
        // way out so we don't leak a membership (Coherence caps at 3 concurrent
        // lobbies per player). Only the success-return paths flip this to true.
        bool keepLobby = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isHost = lobbySession.LobbyOwnerActions != null;

            Report(onProgress, isHost
                ? "Created lobby. Waiting for an opponent..."
                : "Joined lobby. Waiting for the host to start the game...");

            // Hook the play-started push before doing anything else so we never miss it.
            var roomTcs = new TaskCompletionSource<RoomData>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnPlayStarted(string startedLobbyId, RoomData room)
            {
                if (startedLobbyId == lobbySession.LobbyData.Id)
                {
                    roomTcs.TrySetResult(room);
                }
            }
            void OnLobbyDisposedHandler(LobbySession _)
            {
                roomTcs.TrySetException(new MatchmakingException("Lobby was disposed before the game started."));
            }
            lobbyService.OnPlaySessionStarted += OnPlayStarted;
            lobbySession.OnLobbyDisposed += OnLobbyDisposedHandler;
            await using var roomCancelReg = cancellationToken.Register(() => roomTcs.TrySetCanceled(cancellationToken));

            try
            {
                if (isHost)
                {
                    if (lobbySession.LobbyData.Players.Count < MatchSize)
                    {
                        var joinTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        void OnPlayerJoined(LobbySession session, LobbyPlayer player)
                        {
                            if (session.LobbyData.Players.Count >= MatchSize)
                            {
                                joinTcs.TrySetResult(true);
                            }
                        }
                        void OnDisposedDuringWait(LobbySession _)
                        {
                            joinTcs.TrySetException(new MatchmakingException("Lobby was disposed while waiting for an opponent."));
                        }

                        lobbySession.OnPlayerJoined += OnPlayerJoined;
                        lobbySession.OnLobbyDisposed += OnDisposedDuringWait;
                        using var joinCancelReg = cancellationToken.Register(() => joinTcs.TrySetCanceled(cancellationToken));
                        try
                        {
                            bool useBotFallback = fallbackToBotEnabled && botLevels != null && botLevels.Count > 0;

                            if (useBotFallback)
                            {
                                // Race the opponent-join against the fallback timer. Whichever
                                // finishes first wins; we surface cancellation via the token
                                // check so a manual cancel during the wait still throws
                                // OperationCanceledException rather than being mistaken for a
                                // bot fallback.
                                var timeoutTask = Task.Delay(botFallbackTimeout, cancellationToken);
                                var completed = await Task.WhenAny(joinTcs.Task, timeoutTask);
                                cancellationToken.ThrowIfCancellationRequested();

                                if (completed != joinTcs.Task)
                                {
                                    Report(onProgress, "No opponent in time. Falling back to a bot match.");
                                    string botScene = botLevels[UnityEngine.Random.Range(0, botLevels.Count)];
                                    return MatchResult.ForBotFallback(botScene);
                                }

                                await joinTcs.Task;
                            }
                            else
                            {
                                await joinTcs.Task;
                            }
                        }
                        finally
                        {
                            lobbySession.OnPlayerJoined -= OnPlayerJoined;
                            lobbySession.OnLobbyDisposed -= OnDisposedDuringWait;
                        }
                    }

                    SetState(MatchmakingState.OpponentFound);
                    Report(onProgress, "Opponent joined. Starting game session...");

                    try
                    {
                        await AwaitWithTimeout(
                            lobbySession.LobbyOwnerActions.StartGameSessionAsync(
                                maxPlayers: MatchSize,
                                unlistLobby: true,
                                closeLobby: true),
                            GameStartTimeout, "the game session to start", cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (RequestException re)
                    {
                        throw new MatchmakingException($"Failed to start game session: {re.ErrorCode} - {re.Message}", re);
                    }
                    catch (Exception e)
                    {
                        throw new MatchmakingException("Failed to start game session.", e);
                    }
                }
                else
                {
                    // Joiner: we slotted into an already-populated lobby, so the
                    // "opponent" (the host) is by definition present. We just wait
                    // for them to flip the play session on.
                    SetState(MatchmakingState.OpponentFound);
                }

                SetState(MatchmakingState.GameStarting);
                Report(onProgress, "Waiting for room data...");

                RoomData roomData;
                try
                {
                    // The play-started push can be lost (joiner slipping in right
                    // as the host starts/closes the lobby) or never come at all
                    // (we joined an orphaned lobby whose owner is gone) — without
                    // this bound either case stranded the player on "Game starting...".
                    roomData = await AwaitWithTimeout(roomTcs.Task, RoomDataTimeout, "room data", cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (MatchmakingException)
                {
                    // A joined lobby that never delivered its play session is an
                    // orphaned shell — remember it so we never join it again.
                    if (!isHost) _deadLobbyIds.Add(lobbySession.LobbyData.Id);
                    throw;
                }
                catch (Exception e)
                {
                    if (!isHost) _deadLobbyIds.Add(lobbySession.LobbyData.Id);
                    throw new MatchmakingException("Failed while waiting for room data.", e);
                }

                Report(onProgress, "Match ready.");
                // The lobby's "map" attribute is the single source of truth for
                // which scene this match plays on — for the HOST too: if a
                // pre-existing lobby slipped past the acquisition checks, the
                // attribute wins over the local pick, otherwise two clients load
                // different scenes into one room and each sees the union of both
                // levels' entities. A missing attribute is only tolerable for the
                // host (a fresh create whose response didn't echo attributes
                // locally — the acquisition check guarantees no foreign attribute
                // was present); a joiner with no attribute cannot know the scene
                // and must refuse to start.
                string resolvedMap = lobbySession.LobbyData.GetAttribute("map")?.GetStringValue();
                if (string.IsNullOrEmpty(resolvedMap))
                {
                    if (isHost)
                        resolvedMap = createMapName;
                    else
                        throw new MatchmakingException("Joined lobby has no 'map' attribute — refusing to start a match whose scene the clients could disagree on.");
                }
                else if (isHost && resolvedMap != createMapName)
                {
                    UnityEngine.Debug.LogWarning($"[Matchmaker] Pre-existing lobby reused: its map '{resolvedMap}' overrides the locally picked '{createMapName}'.");
                }
                keepLobby = true;
                return new MatchResult(lobbySession, roomData, isHost, resolvedMap);
            }
            finally
            {
                lobbyService.OnPlaySessionStarted -= OnPlayStarted;
                lobbySession.OnLobbyDisposed -= OnLobbyDisposedHandler;
            }
        }
        finally
        {
            if (!keepLobby)
            {
                await AbandonLobbyAsync(lobbySession);
            }
        }
    }

    /// <summary>
    /// Fully abandons a lobby. If we OWN it, it is closed and unlisted first:
    /// an owner-abandoned lobby otherwise lingers listed and open, and every
    /// subsequent search on any client joins it, waits for a host that no longer
    /// exists, and times out — repeatedly, until the server culls it. This is
    /// exactly what happened after bot-fallback matches (host created the lobby,
    /// fell back to the bot, and merely left). Then leaves. Errors logged, never
    /// thrown. Returns whether the membership is confirmed gone.
    /// </summary>
    public static async Task<bool> AbandonLobbyAsync(LobbySession lobby)
    {
        if (lobby is null || lobby.IsDisposed)
        {
            return true;
        }
        if (lobby.LobbyOwnerActions != null)
        {
            try
            {
                await lobby.LobbyOwnerActions.CloseLobbyAsync();
                await lobby.LobbyOwnerActions.UnlistLobbyAsync();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Matchmaker] Failed to close/unlist abandoned lobby: {e.Message}");
            }
        }
        return await LeaveLobbyAsync(lobby);
    }

    /// <summary>
    /// Leaves the given lobby and disposes the session. Safe to call with a null
    /// or already-disposed lobby. Errors are swallowed and logged so this can be
    /// used as a cleanup on disconnect/scene change paths where throwing would
    /// just be noise. Returns whether the membership is confirmed gone — callers
    /// tracking stale memberships (see _lobbyPendingLeave) rely on it.
    /// Prefer <see cref="AbandonLobbyAsync"/> for lobbies we might own.
    /// </summary>
    public static async Task<bool> LeaveLobbyAsync(LobbySession lobby)
    {
        if (lobby is null || lobby.IsDisposed)
        {
            return true;
        }
        try
        {
            await lobby.LeaveLobbyAsync();
            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[Matchmaker] Failed to leave lobby cleanly: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Awaits a task with a hard timeout, surfacing cancellation first. Times out
    /// into MatchmakingException so the caller's failure flow (status + return to
    /// menu) takes over instead of stranding the player.
    /// </summary>
    private static async Task<T> AwaitWithTimeout<T>(Task<T> task, TimeSpan timeout, string what, CancellationToken token)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout, token));
        token.ThrowIfCancellationRequested();
        if (completed != task)
        {
            throw new MatchmakingException($"Timed out after {timeout.TotalSeconds:0}s waiting for {what}.");
        }
        return await task;
    }

    private static async Task AwaitWithTimeout(Task task, TimeSpan timeout, string what, CancellationToken token)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout, token));
        token.ThrowIfCancellationRequested();
        if (completed != task)
        {
            throw new MatchmakingException($"Timed out after {timeout.TotalSeconds:0}s waiting for {what}.");
        }
        await task;
    }

    /// <summary>
    /// True when the lobby FindOrCreate handed us cannot carry a healthy match.
    ///
    /// Owned lobby: must look freshly created — a NON-NULL map attribute that
    /// differs from our pick, or other occupants already present, means our
    /// previous match's lobby came back via lingering membership. A null
    /// attribute is NOT staleness: the create response may simply not echo
    /// attributes locally yet (treating it as stale falsely aborted matchmaking
    /// and, worse, seeded orphan lobbies with every create-then-leave).
    ///
    /// Joined lobbies are NOT judged here: LobbyData.Players proved unreliable at
    /// join-acquisition time (it can under-report membership), and rejecting on it
    /// bounced every legitimate match. An orphaned joined lobby (owner gone) shows
    /// up as a missing play-session push instead, which the bounded room-data wait
    /// (RoomDataTimeout) converts into a clean failure rather than a hang.
    /// </summary>
    private static bool IsUnusableLobby(LobbySession lobby, string createMapName, out string reason)
    {
        if (lobby.LobbyOwnerActions != null)
        {
            var data = lobby.LobbyData;
            string mapAttribute = data.GetAttribute("map")?.GetStringValue();
            if (mapAttribute != null && mapAttribute != createMapName)
            {
                reason = $"own lobby carries a foreign map attribute '{mapAttribute}' (expected '{createMapName}')";
                return true;
            }
            if (data.Players.Count >= MatchSize)
            {
                reason = "own lobby is already full at acquisition";
                return true;
            }
        }
        else if (_deadLobbyIds.Contains(lobby.LobbyData.Id))
        {
            reason = "this lobby already failed to deliver a game session before (orphaned shell)";
            return true;
        }

        reason = null;
        return false;
    }

    private static void SetState(MatchmakingState next)
    {
        if (State == next)
        {
            return;
        }
        State = next;
        var handler = StateChanged;
        if (handler == null)
        {
            return;
        }
        try
        {
            handler.Invoke(next);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }
    }

    private static void Report(Action<string> onProgress, string message)
    {
        if (onProgress is null)
        {
            return;
        }
        try
        {
            onProgress.Invoke(message);
        }
        catch
        {
            // Progress callback failures must never break matchmaking.
        }
    }
}

public class MatchmakingException : Exception
{
    public MatchmakingException(string message) : base(message) { }
    public MatchmakingException(string message, Exception innerException) : base(message, innerException) { }
}
