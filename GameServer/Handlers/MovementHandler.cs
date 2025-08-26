using GameServerApp.Models;
using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

public class MovementHandler : MessageHandler<MoveRequest>
{
    private readonly DatabaseHelper _database;

    public MovementHandler(DatabaseHelper database) : base(MessageType.MoveRequest)
    {
        _database = database;
    }

    protected override Task HandleAsync(Session session, MoveRequest message)
    {
        var character = new Character(message.Name);
        character.X += message.Dx;
        character.Y += message.Dy;
        _database.SavePlayer(character);
        var response = new MoveResponse { X = character.X, Y = character.Y };
        return session.SendAsync(MessageType.MoveResponse, response);
    }
}
