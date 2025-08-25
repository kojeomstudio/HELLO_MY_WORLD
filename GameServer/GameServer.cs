using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using GameServerApp.Models;
using GameServerApp.Database;
using GameServerApp.Handlers;

namespace GameServerApp
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly DatabaseHelper _database;
        private readonly MovementHandler _movementHandler;

        public GameServer(int port = 9000, string databaseFile = "game.db")
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _database = new DatabaseHelper(databaseFile);
            _movementHandler = new MovementHandler();
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("Server started on port {0}", ((_listener.LocalEndpoint as IPEndPoint)?.Port));

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("Client connected.");

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            string? name = await reader.ReadLineAsync();
            var character = new Character(name ?? "Unknown");
            _database.SavePlayer(character);
            await writer.WriteLineAsync($"HELLO {character.Name}");

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var parts = line.Split(' ');
                if (parts[0] == "MOVE" && parts.Length == 3 &&
                    double.TryParse(parts[1], out double dx) &&
                    double.TryParse(parts[2], out double dy))
                {
                    _movementHandler.Move(character, dx, dy, _database);
                    await writer.WriteLineAsync($"POS {character.X} {character.Y}");
                }
            }
        }
    }
}
