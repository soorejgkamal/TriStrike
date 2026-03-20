namespace TriStrike.Services;

/// <summary>
/// Scoped per-circuit service that identifies the current player's browser session.
/// </summary>
public class SessionService
{
    public string SessionId { get; private set; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// Overrides the auto-generated session ID with a value restored from
    /// persistent storage (e.g. a cookie set by the host page). Must be called
    /// before any component reads <see cref="SessionId"/>.
    /// </summary>
    public void SetSessionId(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
            SessionId = id;
    }
}
