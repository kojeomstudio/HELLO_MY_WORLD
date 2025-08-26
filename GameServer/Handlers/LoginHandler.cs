using GameServerApp.Models;
using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

public class LoginHandler : MessageHandler<LoginRequest>
{
    private readonly DatabaseHelper _database;

    public LoginHandler(DatabaseHelper database) : base(MessageType.LoginRequest)
    {
        _database = database;
    }

    protected override Task HandleAsync(Session session, LoginRequest message)
    {
        var character = new Character(message.Name);
        _database.SavePlayer(character);
        var response = new LoginResponse { Success = true, Message = $"HELLO {message.Name}" };
        return session.SendAsync(MessageType.LoginResponse, response);
    }
}
