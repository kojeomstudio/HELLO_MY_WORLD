using System.Net;
using System.Net.Sockets;
using SharedProtocol;

namespace SimpleTestClient;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, 9000);
        var session = new Session(client);

        var login = new LoginRequest { Name = "Tester" };
        await session.SendAsync(MessageType.LoginRequest, login);

        var move = new MoveRequest { Name = "Tester", Dx = 1, Dy = 1 };
        await session.SendAsync(MessageType.MoveRequest, move);

        var (type, message) = await session.ReceiveAsync();
        if (type == MessageType.MoveResponse && message is MoveResponse res)
        {
            Console.WriteLine($"Position: {res.X}, {res.Y}");
        }
    }
}
