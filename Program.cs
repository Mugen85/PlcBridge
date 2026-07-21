using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console; 

class Program
{
    private static readonly Random random = new Random();

    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]Errore:[/ red] Specificare [yellow]server[/] o [yellow]client[/].");
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
        AnsiConsole.MarkupLine("[bold green]PLC/SERVER Virtuale attivo sulla porta 5000...[/]");
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();

        while (true)
        {
            using TcpClient client = await listener.AcceptTcpClientAsync();
            using NetworkStream stream = client.GetStream();
            
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            AnsiConsole.MarkupLine($"[dim]Richiesta ricevuta:[/] [yellow]{command}[/]");

            string responseData = command switch
            {
                "READ_PRESSURE" => $"PRESSURE: {Math.Round(10.0 + (random.NextDouble() * 5.0), 1)} BAR",
                "READ_TEMP" => $"TEMP: {Math.Round(20.0 + (random.NextDouble() * 10.0), 1)} C",
                _ => "ERROR: Unknown Command"
            };

            byte[] response = Encoding.UTF8.GetBytes(responseData);
            await stream.WriteAsync(response, 0, response.Length);
        }
    }

    static async Task RunClient()
    {
        AnsiConsole.MarkupLine("[bold blue]CLIENT/MONITOR di Supervisione[/]");
        
        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);

        using NetworkStream stream = client.GetStream();
        
        var command = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Seleziona il comando da inviare:")
                .PageSize(10)
                .AddChoices(new[] {
                    "READ_PRESSURE",
                    "READ_TEMP"
                }));
        
        byte[] commandBytes = Encoding.UTF8.GetBytes(command);
        await stream.WriteAsync(commandBytes, 0, commandBytes.Length);

        byte[] buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        var table = new Table();
        table.AddColumn("Stato");
        table.AddColumn("Valore");
        table.AddRow("Comando inviato", command);
        table.AddRow("Risposta PLC", $"[bold]{message}[/]");
        
        AnsiConsole.Write(table);
    }
}