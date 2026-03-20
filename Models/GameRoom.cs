using TriStrike.Services;

namespace TriStrike.Models;

public class GameRoom
{
    public string RoomCode { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public List<RoomPlayer> Players { get; private set; } = new();
    public bool IsStarted { get; set; }
    public bool IsLocked { get; set; }
    public GameState? GameState { get; set; }

    public bool IsFull => Players.Count >= MaxPlayers;
    public bool IsWaiting => !IsStarted;
}
