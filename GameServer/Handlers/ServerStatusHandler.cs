using System;
using GameServerApp.Systems;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// Handles incoming status requests and returns runtime metrics to authenticated clients.
/// </summary>
public class ServerStatusHandler : MessageHandler<ServerStatusRequest>
{
    private readonly SessionManager _sessions;
    private readonly ServerMetricsService _metrics;

    public ServerStatusHandler(SessionManager sessions, ServerMetricsService metrics)
        : base(MessageType.ServerStatusRequest)
    {
        _sessions = sessions;
        _metrics = metrics;
    }

    protected override async Task HandleAsync(Session session, ServerStatusRequest message)
    {
        if (!_sessions.ValidateSession(session))
        {
            Console.WriteLine("Received server status request from unauthenticated session.");
            return;
        }

        if (!string.IsNullOrEmpty(message.SessionToken) &&
            !string.Equals(session.SessionToken, message.SessionToken, StringComparison.Ordinal))
        {
            Console.WriteLine($"Server status token mismatch for {session.UserName ?? "unknown"}.");
            return;
        }

        var snapshot = _metrics.CaptureStatus();

        var response = new ServerStatusResponse
        {
            OnlinePlayers = snapshot.OnlinePlayers,
            ServerVersion = snapshot.ServerVersion,
            ServerUptime = snapshot.UptimeMilliseconds,
            ContainerHashMismatches = snapshot.ContainerHashMismatches,
            TotalTrackedChunks = snapshot.TotalTrackedChunks,
            ActiveChunkResidencyPlayers = snapshot.PlayersWithChunkResidency,
            PeakChunksPerPlayer = snapshot.PeakChunksPerPlayer,
            BusiestChunkPlayer = snapshot.BusiestChunkPlayer
        };

        await session.SendAsync(MessageType.ServerStatusResponse, response);
    }
}
