using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: dotnet run <server|client>");
            return;
        }

        string mode = args[0].ToLower();

        if (mode == "server")
        {
            await RunServer();
        }
        else if (mode == "client")
        {
            await RunClient();
        }
    }

    static async Task RunServer()
    {
        Console.WriteLine("[SERVER/PLC] In attesa di comandi sulla porta 5000...");
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();

        while (true)
        {
            using TcpClient client = await listener.AcceptTcpClientAsync();
            using NetworkStream stream = client.GetStream();
            
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            Console.WriteLine($"[SERVER/PLC] Ricevuto comando: {command}");

            if (command == "READ_PRESSURE")
            {
                string data = "PRESSURE: 12.5 BAR";
                byte[] response = Encoding.UTF8.GetBytes(data);
                await stream.WriteAsync(response, 0, response.Length);
                Console.WriteLine($"[SERVER/PLC] Dato inviato: {data}");
            }
            else if (command == "READ_TEMP")
            {
                string data = "TEMP: 24.5 C";
                byte[] response = Encoding.UTF8.GetBytes(data);
                await stream.WriteAsync(response, 0, response.Length);
                Console.WriteLine($"[SERVER/PLC] Dato inviato: {data}");
            }
            else
            {
                string error = "ERROR: Unknown Command";
                byte[] response = Encoding.UTF8.GetBytes(error);
                await stream.WriteAsync(response, 0, response.Length);
                Console.WriteLine($"[SERVER/PLC] Errore inviato: {error}");
            }
        }
    }

    static async Task RunClient()
    {
        Console.WriteLine("[CLIENT/MONITOR] Connessione al PLC...");
        
        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);

        using NetworkStream stream = client.GetStream();
        
        // Input interattivo
        Console.Write("Inserisci comando (es. READ_PRESSURE, READ_TEMP): ");
        // Console.ReadLine() can return null; provide a safe default to avoid nullable warnings
        string command = Console.ReadLine() ?? string.Empty;
        
        byte[] commandBytes = Encoding.UTF8.GetBytes(command);
        await stream.WriteAsync(commandBytes, 0, commandBytes.Length);

        byte[] buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"[CLIENT/MONITOR] Risposta ricevuta: {message}");
    }
}