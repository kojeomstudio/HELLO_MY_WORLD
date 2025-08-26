using GameServerApp.Models;
using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

public class MovementHandler : MessageHandler<MoveRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;

    public MovementHandler(DatabaseHelper database, SessionManager sessions) : base(MessageType.MoveRequest)
    {
        _database = database;
        _sessions = sessions;
    }

    protected override Task HandleAsync(Session session, MoveRequest message)
    {
        // Use the session's bound user name to avoid trusting client-provided data.
        var name = session.UserName ?? message.Name;

        // Ignore movement commands from unknown sessions.
        if (_sessions.GetSession(name) != session)
            return Task.CompletedTask;

        var character = new Character(name);
        character.X += message.Dx;
        character.Y += message.Dy;
        _database.SavePlayer(character);
        var response = new MoveResponse { X = character.X, Y = character.Y };
        return session.SendAsync(MessageType.MoveResponse, response);
    }
}
