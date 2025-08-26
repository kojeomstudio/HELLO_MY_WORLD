using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GameServerApp.Database;
using GameServerApp.Handlers;
using SharedProtocol;

namespace GameServerApp
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly DatabaseHelper _database;
        private readonly MessageDispatcher _dispatcher;
        private readonly SessionManager _sessions;

        public GameServer(int port = 9000, string databaseFile = "game.db")
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _database = new DatabaseHelper(databaseFile);
            _dispatcher = new MessageDispatcher();
            _sessions = new SessionManager();
            _dispatcher.Register(new LoginHandler(_database, _sessions));
            _dispatcher.Register(new MovementHandler(_database, _sessions));
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
            var session = new Session(client);

            try
            {
                while (true)
                {
                    var (type, message) = await session.ReceiveAsync();
                    await _dispatcher.DispatchAsync(session, type, message);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Client disconnected.");
            }
            finally
            {
                _sessions.Remove(session);
                client.Close();
            }
        }
    }
}
