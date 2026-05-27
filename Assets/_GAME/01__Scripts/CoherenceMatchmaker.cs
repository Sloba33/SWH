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

        LobbySession lobbySession;
        try
        {
            lobbySession = await lobbyService.FindOrCreateLobbyAsync(findOptions, createOptions, cancellationToken);
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
                        await lobbySession.LobbyOwnerActions.StartGameSessionAsync(
                            maxPlayers: MatchSize,
                            unlistLobby: true,
                            closeLobby: true);
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
                    roomData = await roomTcs.Task;
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
                    throw new MatchmakingException("Failed while waiting for room data.", e);
                }

                Report(onProgress, "Match ready.");
                keepLobby = true;
                // The host knows their own map (createMapName); the joiner reads it from
                // the lobby attribute the host set. mapName is the only fallback if the
                // attribute is somehow absent — which shouldn't happen since we always
                // write it on create.
                string resolvedMap = isHost
                    ? createMapName
                    : (lobbySession.LobbyData.GetAttribute("map")?.GetStringValue() ?? mapName);
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
                await LeaveLobbyAsync(lobbySession);
            }
        }
    }

    /// <summary>
    /// Leaves the given lobby and disposes the session. Safe to call with a null
    /// or already-disposed lobby. Errors are swallowed and logged so this can be
    /// used as a fire-and-forget cleanup on disconnect/scene change paths where
    /// throwing would just be noise.
    /// </summary>
    public static async Task LeaveLobbyAsync(LobbySession lobby)
    {
        if (lobby is null || lobby.IsDisposed)
        {
            return;
        }
        try
        {
            await lobby.LeaveLobbyAsync();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[Matchmaker] Failed to leave lobby cleanly: {e.Message}");
        }
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
