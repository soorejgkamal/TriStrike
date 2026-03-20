namespace TriStrike.Models;

public class RoomPlayer
{
    public string Name { get; set; } = string.Empty;
    public int PlayerIndex { get; set; }
    public bool IsHost { get; set; }
    public string SessionId { get; set; } = string.Empty;
}
