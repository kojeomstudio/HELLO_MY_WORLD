using GameServerApp.Models;
using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

public class LoginHandler : MessageHandler<LoginRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;

    public LoginHandler(DatabaseHelper database, SessionManager sessions) : base(MessageType.LoginRequest)
    {
        _database = database;
        _sessions = sessions;
    }

    protected override Task HandleAsync(Session session, LoginRequest message)
    {
        // Bind the user name to the session and register it for tracking.
        session.UserName = message.Name;
        _sessions.Add(session);

        var character = new Character(message.Name);
        _database.SavePlayer(character);
        var response = new LoginResponse { Success = true, Message = $"HELLO {message.Name}" };
        return session.SendAsync(MessageType.LoginResponse, response);
    }
}
