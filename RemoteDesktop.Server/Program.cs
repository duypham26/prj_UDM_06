using System;
using System.Threading.Tasks;
using RemoteDesktop.Server.Network;
using RemoteDesktop.Shared.Network;

namespace RemoteDesktop.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Remote Desktop Server");
            Console.WriteLine("====================");

            string ip = "127.0.0.1";
            int port = NetworkConstants.DefaultPort;
            string password = "admin123";

            Console.WriteLine($"Server IP: {ip}");
            Console.WriteLine($"Port: {port}");
            Console.WriteLine($"Password: {password}");
            Console.WriteLine("Waiting for connections...");

            var server = new RemoteDesktopServer();
            server.ClientConnected += (s, e) => Console.WriteLine($"Client connected: {e.ClientIP}");
            server.ClientDisconnected += (s, e) => Console.WriteLine($"Client disconnected: {e.ClientIP}");
            server.ErrorOccurred += (s, e) => Console.WriteLine($"Error: {e.Message}");

            try
            {
                await server.StartServerAsync(ip, port, password);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }
    }
}