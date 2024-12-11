using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Xml; //Ставится отдельно. В базовый Microsoft.Extensions.Configuration не входит

namespace ConfigurationManagerTestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Создание ConfigurationBuilder и добавление источников конфигурации
            var configuration = new ConfigurationBuilder()
                .AddXmlFile("appsettings.xml")
                .Build();

            // Чтение конфигурационных данных
            var serverIpAddress = IPAddress.Parse(configuration["Server:IpAddress"]);
            var serverPort = int.Parse(configuration["Server:Port"]);
            var clientIpAddress = IPAddress.Parse(configuration["Client:IpAddress"]);
            var clientPort = int.Parse(configuration["Client:Port"]);

            Console.WriteLine("Что лежит в configuration :");
            foreach(var e in configuration.AsEnumerable())
                Console.WriteLine(e);
            Console.WriteLine();
            // Запуск сервера
            var server = new TcpServer(serverIpAddress, serverPort);
            _ = server.StartAsync();

            // Запуск клиента
            var client = new ConfigurationManagerTestApp.TcpClient(clientIpAddress, clientPort);
            await client.ConnectAsync();

            // Клиент отправляет сообщение серверу
            await client.SendMessageAsync("Hello, Server!");

            // Сервер отправляет ответ клиенту
            await server.SendMessageToClientAsync("Hello, Client!");

            // Ожидание завершения работы
            await Task.Delay(1000);
        }
    }

    public class TcpServer
    {
        private readonly TcpListener _listener;
        private System.Net.Sockets.TcpClient _client;

        public TcpServer(IPAddress ipAddress, int port)
        {
            _listener = new TcpListener(ipAddress, port);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("Server started.");

            // Ожидание подключения клиента
            _client = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            // Запуск асинхронного чтения сообщений от клиента
            _ = HandleClientAsync();
        }

        private async Task HandleClientAsync()
        {
            var stream = _client.GetStream();
            var buffer = new byte[256];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Server received: {message}");
            }
        }

        public async Task SendMessageToClientAsync(string message)
        {
            if (_client == null || !_client.Connected) return;

            var stream = _client.GetStream();
            var data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"Server sent: {message}");
        }
    }

    public class TcpClient
    {
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private System.Net.Sockets.TcpClient _client;

        public TcpClient(IPAddress ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public async Task ConnectAsync()
        {
            _client = new System.Net.Sockets.TcpClient();
            await _client.ConnectAsync(_ipAddress, _port);
            Console.WriteLine("Client connected to server.");

            // Запуск асинхронного чтения сообщений от сервера
            _ = HandleServerAsync();
        }

        private async Task HandleServerAsync()
        {
            var stream = _client.GetStream();
            var buffer = new byte[256];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Client received: {message}");
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!_client.Connected) return;

            var stream = _client.GetStream();
            var data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"Client sent: {message}");
        }
    }
}