namespace TriStrike.Services;

/// <summary>
/// Scoped per-circuit service that identifies the current player's browser session.
/// </summary>
public class SessionService
{
    public string SessionId { get; } = Guid.NewGuid().ToString("N")[..12];
}
