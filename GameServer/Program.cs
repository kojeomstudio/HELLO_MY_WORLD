using System.Threading.Tasks;

namespace GameServerApp
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var server = new GameServer();
            await server.StartAsync();
        }
    }
}
