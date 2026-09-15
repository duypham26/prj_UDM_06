using System;
using System.Threading;
using NewRemoteDesktop.Server.Network;
using NewRemoteDesktop.Server.Services;

int port = 5000;

var connectionService = new ConnectionService();
var serverSocket = new ServerSocket(port);

serverSocket.OnClientConnected += tcpClient =>
{
    string clientId = Guid.NewGuid().ToString();
    var handler = new ClientHandler(tcpClient);

    connectionService.AddClient(clientId, handler);
    Console.WriteLine($"Client connected: {clientId} - {tcpClient.Client.RemoteEndPoint}");

    handler.OnPacketReceived += (packetType, payload) =>
    {
        Console.WriteLine($"Received packet from {clientId}: type={packetType}, length={(payload?.Length ?? 0)}");
        // Echo the packet back to the sender as a simple example
        _ = handler.SendAsync(packetType, payload);
    };

    handler.OnDisconnected += _ =>
    {
        Console.WriteLine($"Client disconnected: {clientId}");
        connectionService.RemoveClient(clientId);
    };

    handler.StartListening();
};

serverSocket.Start();
Console.WriteLine($"Server started on port {port}. Press Ctrl+C to stop.");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    Console.WriteLine("Shutdown requested...");
    cts.Cancel();
};

try
{
    cts.Token.WaitHandle.WaitOne();
}
finally
{
    Console.WriteLine("Stopping server...");
    serverSocket.Stop();
    connectionService.DisconnectAll();
    Console.WriteLine("Server stopped.");
}
