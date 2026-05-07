using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Coherence.Cloud;
using Coherence.Runtime;

public static class CoherenceMatchmaker
{
    private const int MatchSize = 2;

    public readonly struct MatchResult
    {
        public LobbySession Lobby { get; }
        public RoomData Room { get; }
        public bool IsHost { get; }

        public MatchResult(LobbySession lobby, RoomData room, bool isHost)
        {
            Lobby = lobby;
            Room = room;
            IsHost = isHost;
        }
    }

    /// <summary>
    /// Finds or creates a 1v1 lobby, waits until the game session starts,
    /// and returns the room to connect to (call <c>bridge.JoinRoom(result.Room)</c>).
    /// The lobby is closed but not left, so it can be used for in-game reconnection.
    /// <para>
    /// <c>onProgress</c> is an optional callback that receives a human-readable
    /// status string each time the matchmaker advances to the next step.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when input parameters are invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is signalled.</exception>
    /// <exception cref="MatchmakingException">Thrown for any matchmaking failure (login, lobby, room).</exception>
    public static async Task<MatchResult> FindMatchAsync(
        string mapName,
        int skillLevel,
        int minOpponentSkill,
        int maxOpponentSkill,
        string region = null,
        Action<string> onProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mapName))
        {
            throw new ArgumentException("Map name is required.", nameof(mapName));
        }
        if (minOpponentSkill > maxOpponentSkill)
        {
            throw new ArgumentException(
                $"minOpponentSkill ({minOpponentSkill}) must be <= maxOpponentSkill ({maxOpponentSkill}).",
                nameof(minOpponentSkill));
        }

        Report(onProgress, "Logging in to coherence Cloud...");

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
        var filter = new LobbyFilter()
            .WithAnd()
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, mapName)
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
                new("map",       mapName,          StringAttributeIndex.s1, StringAggregator.None, isPublic: true),
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
            // Reconnection short-circuit: if the lobby already has a room, return it immediately.
            if (lobbySession.LobbyData.RoomData is { } existingRoom)
            {
                Report(onProgress, "Reconnecting to existing match...");
                return new MatchResult(lobbySession, existingRoom, isHost);
            }

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
                        await joinTcs.Task;
                    }
                    finally
                    {
                        lobbySession.OnPlayerJoined -= OnPlayerJoined;
                        lobbySession.OnLobbyDisposed -= OnDisposedDuringWait;
                    }
                }

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
            return new MatchResult(lobbySession, roomData, isHost);
        }
        finally
        {
            lobbyService.OnPlaySessionStarted -= OnPlayStarted;
            lobbySession.OnLobbyDisposed -= OnLobbyDisposedHandler;
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