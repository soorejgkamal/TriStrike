using Microsoft.AspNetCore.SignalR;
using TriStrike.Services;

namespace TriStrike.Hubs;

/// <summary>
/// SignalR hub for TriStrike multiplayer. Clients may connect here directly;
/// Blazor Server components use RoomService callbacks for UI updates, while
/// this hub broadcasts lightweight "room changed" signals so that any additional
/// JavaScript clients can react in real time.
/// </summary>
public class GameHub : Hub
{
    private readonly RoomService _roomService;

    public GameHub(RoomService roomService)
    {
        _roomService = roomService;
    }

    /// <summary>Adds the caller to a room's SignalR group.</summary>
    public async Task JoinRoomGroup(string roomCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode.ToUpper());
    }

    /// <summary>Removes the caller from a room's SignalR group.</summary>
    public async Task LeaveRoomGroup(string roomCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode.ToUpper());
    }

    /// <summary>
    /// Processes a cell-strike action on behalf of the connected player.
    /// The server validates turn ownership before applying the move.
    /// </summary>
    public async Task StrikeCell(string roomCode, string sessionId, int row, int col)
    {
        var error = await _roomService.StrikeCellAsync(roomCode, sessionId, row, col);
        if (error == null)
        {
            await Clients.Group(roomCode.ToUpper())
                .SendAsync("GameStateUpdated", roomCode);
        }
    }
}
