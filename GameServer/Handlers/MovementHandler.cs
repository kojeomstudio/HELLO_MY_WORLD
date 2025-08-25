using GameServerApp.Models;
using GameServerApp.Database;

namespace GameServerApp.Handlers
{
    public class MovementHandler
    {
        public void Move(Character character, double dx, double dy, DatabaseHelper database)
        {
            character.X += dx;
            character.Y += dy;
            database.SavePlayer(character);
        }
    }
}
