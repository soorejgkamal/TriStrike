using TriStrike.Models;

namespace TriStrike.Services;

/// <summary>
/// Singleton service that manages in-memory game rooms and broadcasts state changes
/// to all connected Blazor Server circuits in a room.
/// </summary>
public class RoomService
{
    private readonly Dictionary<string, GameRoom> _rooms =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<(string SessionId, Func<Task> Callback)>> _callbacks =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    // ── Room lifecycle ────────────────────────────────────────────────────────

    /// <summary>Creates a new room and returns it.</summary>
    public GameRoom CreateRoom(int maxPlayers, string hostName, string sessionId)
    {
        var code = GenerateCode();
        var room = new GameRoom { RoomCode = code, MaxPlayers = maxPlayers };
        var host = new RoomPlayer
        {
            Name = string.IsNullOrWhiteSpace(hostName) ? "Player 1" : hostName.Trim(),
            PlayerIndex = 0,
            IsHost = true,
            SessionId = sessionId
        };
        room.Players.Add(host);

        lock (_lock)
        {
            _rooms[code] = room;
        }

        return room;
    }

    /// <summary>
    /// Adds a player to an existing room. Returns the updated room or an error message.
    /// If the room becomes full, the game is automatically started.
    /// </summary>
    public (GameRoom? Room, string? Error) JoinRoom(string code, string playerName, string sessionId)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(code.Trim(), out var room))
                return (null, "Room not found. Check the room code and try again.");

            if (room.IsLocked)
                return (null, "This room has already started. It is no longer accepting new players.");

            if (room.IsFull)
                return (null, "This room is full.");

            // Reconnect: same session already in the room
            var existing = room.Players.FirstOrDefault(p => p.SessionId == sessionId);
            if (existing != null)
                return (room, null);

            var player = new RoomPlayer
            {
                Name = string.IsNullOrWhiteSpace(playerName)
                    ? $"Player {room.Players.Count + 1}"
                    : playerName.Trim(),
                PlayerIndex = room.Players.Count,
                IsHost = false,
                SessionId = sessionId
            };
            room.Players.Add(player);

            if (room.IsFull)
            {
                room.IsStarted = true;
                room.IsLocked = true;
                var gameState = new GameState();
                gameState.SetupGame(room.MaxPlayers, room.Players.Select(p => p.Name).ToList());
                room.GameState = gameState;
            }

            return (room, null);
        }
    }

    /// <summary>Returns a room by code, or null if not found.</summary>
    public GameRoom? GetRoom(string code)
    {
        lock (_lock)
        {
            return _rooms.TryGetValue(code.Trim(), out var room) ? room : null;
        }
    }

    /// <summary>Removes a room and its subscriptions.</summary>
    public void RemoveRoom(string code)
    {
        lock (_lock)
        {
            _rooms.Remove(code);
            _callbacks.Remove(code);
        }
    }

    // ── Game actions ──────────────────────────────────────────────────────────

    /// <summary>
    /// Validates and processes a cell strike for the given session.
    /// Returns an error message if the action is invalid, or null on success.
    /// </summary>
    public async Task<string?> StrikeCellAsync(string roomCode, string sessionId, int row, int col)
    {
        GameRoom? room;
        lock (_lock)
        {
            room = _rooms.TryGetValue(roomCode.Trim(), out var r) ? r : null;
        }

        if (room?.GameState == null) return "Game not found.";
        if (room.GameState.GameOver) return "Game is already over.";

        var player = room.Players.FirstOrDefault(p => p.SessionId == sessionId);
        if (player == null) return "Player not found in room.";
        if (player.PlayerIndex != room.GameState.CurrentPlayerIndex) return "It is not your turn.";

        room.GameState.StrikeCell(row, col);
        await NotifyRoomChangedAsync(roomCode);
        return null;
    }

    public async Task RestartGameAsync(string roomCode, string sessionId)
    {
        GameRoom? room;
        lock (_lock)
        {
            room = _rooms.TryGetValue(roomCode.Trim(), out var r) ? r : null;
        }

        if (room?.GameState == null) return;

        var player = room.Players.FirstOrDefault(p => p.SessionId == sessionId);
        if (player?.IsHost != true) return;

        room.GameState.RestartGame();
        await NotifyRoomChangedAsync(roomCode);
    }

    // ── Subscription / notification ───────────────────────────────────────────

    /// <summary>
    /// Subscribes a callback to be invoked when a room's state changes.
    /// Only one callback per session is kept; re-subscribing replaces the old one.
    /// </summary>
    public void Subscribe(string roomCode, string sessionId, Func<Task> callback)
    {
        lock (_lock)
        {
            var key = roomCode.Trim();
            if (!_callbacks.ContainsKey(key))
                _callbacks[key] = new();

            _callbacks[key].RemoveAll(c => c.SessionId == sessionId);
            _callbacks[key].Add((sessionId, callback));
        }
    }

    /// <summary>Removes a session's callback subscription from a room.</summary>
    public void Unsubscribe(string roomCode, string sessionId)
    {
        lock (_lock)
        {
            var key = roomCode.Trim();
            if (_callbacks.ContainsKey(key))
                _callbacks[key].RemoveAll(c => c.SessionId == sessionId);
        }
    }

    /// <summary>Invokes all registered callbacks for a room (fan-out notification).</summary>
    public async Task NotifyRoomChangedAsync(string roomCode)
    {
        List<Func<Task>> callbacks;
        lock (_lock)
        {
            var key = roomCode.Trim();
            callbacks = _callbacks.TryGetValue(key, out var list)
                ? list.Select(c => c.Callback).ToList()
                : new();
        }

        var tasks = callbacks.Select(cb => Task.Run(async () =>
        {
            try { await cb(); }
            catch { /* ignore errors from individual subscriber callbacks */ }
        }));

        await Task.WhenAll(tasks);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        string code;
        lock (_lock)
        {
            do
            {
                code = new string(Enumerable.Range(0, 6)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());
            } while (_rooms.ContainsKey(code));
        }
        return code;
    }
}
