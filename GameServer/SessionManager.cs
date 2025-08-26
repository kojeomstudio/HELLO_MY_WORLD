using System.Collections.Concurrent;
using SharedProtocol;

namespace GameServerApp;

/// <summary>
/// Thread-safe helper to manage active sessions and their associated user names.
/// </summary>
public class SessionManager
{
    // Map of user name to active session
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    /// <summary>
    /// Registers a session after a successful login. The session must have <see cref="Session.UserName"/> set.
    /// </summary>
    public void Add(Session session)
    {
        if (string.IsNullOrEmpty(session.UserName))
            throw new InvalidOperationException("Session must have a user name before registration.");
        _sessions[session.UserName] = session;
    }

    /// <summary>
    /// Removes the session when the client disconnects.
    /// </summary>
    public void Remove(Session session)
    {
        if (!string.IsNullOrEmpty(session.UserName))
            _sessions.TryRemove(session.UserName, out _);
    }

    /// <summary>
    /// Retrieves the session by user name if connected.
    /// </summary>
    public Session? GetSession(string name) => _sessions.TryGetValue(name, out var session) ? session : null;

    /// <summary>
    /// Gets a snapshot of currently connected user names.
    /// </summary>
    public IReadOnlyCollection<string> ConnectedUsers => _sessions.Keys.ToList();
}
