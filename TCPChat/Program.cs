using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace TCPChat
{
    class ChatServer
    {
        private TcpListener server;
        private List<ClientInfo> clients = new List<ClientInfo>();

        public void Start()
        {
            server = new TcpListener(IPAddress.Any, 8888);
            server.Start();

            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;

            NetworkStream stream = client.GetStream();

            // Получаем имя клиента
            string clientName = ReadMessage(stream);
            Console.WriteLine("Клиент подключен: " + clientName);

            // Добавляем клиента в список активных клиентов
            ClientInfo clientInfo = new ClientInfo(client, clientName);
            clients.Add(clientInfo);

            // Рассылаем сообщение о подключении нового клиента всем клиентам
            SendMessageToAll(clientName + " join.", null);

            // Отправляем список подключенных клиентов новому клиенту
            SendConnectedClients(client);

            while (true)
            {
                string message = ReadMessage(stream);
                Console.WriteLine(clientName + ": " + message);

                // Проверяем, является ли сообщение личным
                if (message.StartsWith("/"))
                {
                    SendPrivateMessage(clientInfo, message);
                }
                else
                {
                    // Рассылаем сообщение всем клиентам, кроме отправителя
                    SendMessageToAll(message, clientInfo);
                }
            }
        }

        private void SendMessageToAll(string message, ClientInfo senderClient)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);

            foreach (ClientInfo clientInfo in clients)
            {
                if (clientInfo != senderClient)
                {
                    NetworkStream stream = clientInfo.Client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                }
            }
        }

        private void SendConnectedClients(TcpClient client)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Clients:");

            foreach (ClientInfo clientInfo in clients)
            {
                sb.AppendLine(clientInfo.Name);
            }

            string connectedClientsMessage = sb.ToString();
            SendMessage(connectedClientsMessage, client);
        }

        private void SendPrivateMessage(ClientInfo senderClient, string message)
        {
            // Извлекаем имя целевого клиента из сообщения
            string targetClientName = message.Substring(1);

            // Находим информацию о целевом клиенте
            ClientInfo targetClient = clients.Find(c => c.Name == targetClientName);

            if (targetClient != null)
            {
                // Отправляем личное сообщение целевому клиенту
                SendMessage(senderClient.Name + " (self): " + message, targetClient.Client);
            }
            else
            {
                // Отправляем уведомление отправителю, если целевой клиент не найден
                SendMessage("Клиент с именем '" + targetClientName + "' не найден.", senderClient.Client);
            }
        }

        private void SendMessage(string message, TcpClient client)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            NetworkStream stream = client.GetStream();
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }

        private string ReadMessage(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }

        public void Stop()
        {
            server.Stop();

            foreach (ClientInfo clientInfo in clients)
            {
                clientInfo.Client.Close();
            }

            Console.WriteLine("Сервер остановлен.");
        }
    }

    class ClientInfo
    {
        public TcpClient Client { get; }
        public string Name { get; }

        public ClientInfo(TcpClient client, string name)
        {
            Client = client;
            Name = name;
        }
    }

    class ChatClient
    {
        private TcpClient client;

        public void Start()
        {
            client = new TcpClient();
            client.Connect(IPAddress.Loopback, 8888);

            Console.WriteLine("Введите ваше имя:");
            string clientName = Console.ReadLine();

            SendMessage(clientName);

            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();

            while (true)
            {
                string message = Console.ReadLine();
                SendMessage(message);
            }
        }

        private void ReceiveMessages()
        {
            NetworkStream stream = client.GetStream();

            while (true)
            {
                string message = ReadMessage(stream);
                Console.WriteLine("Сервер: " + message);
            }
        }

        private void SendMessage(string message)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }

        private string ReadMessage(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }

        public void Stop()
        {
            client.Close();
            Console.WriteLine("Подключение закрыто.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Выберите режим:");
            Console.WriteLine("1. Сервер");
            Console.WriteLine("2. Клиент");

            int mode = int.Parse(Console.ReadLine());

            if (mode == 1)
            {
                ChatServer server = new ChatServer();
                server.Start();
            }
            else if (mode == 2)
            {
                ChatClient client = new ChatClient();
                client.Start();
            }
            else
            {
                Console.WriteLine("Некорректный режим.");
            }
        }
    }

}
