using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console; 

class Program
{
    private static readonly Random random = new Random();
    
    // Stato interno del PLC simulato (memoria dei registri)
    private static bool isPumpRunning = false;
    private static double currentPressure = 12.0;
    private static double currentTemperature = 22.0;

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
        AnsiConsole.MarkupLine("[bold green]PLC/SERVER Virtuale attivo sulla porta 5000 (Stateful Mode)...[/]");
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

            // Aggiornamento simulato dei valori fisici con fluttuazione
            currentPressure = Math.Round(10.0 + (random.NextDouble() * 5.0), 1);
            currentTemperature = Math.Round(20.0 + (random.NextDouble() * 10.0), 1);

            // Elaborazione dei comandi (Lettura e Scrittura / Attuatori)
            string responseData = command switch
            {
                "READ_PRESSURE" => $"PRESSURE: {currentPressure} BAR | PUMP: {(isPumpRunning ? "ON" : "OFF")}",
                "READ_TEMP" => $"TEMP: {currentTemperature} C | PUMP: {(isPumpRunning ? "ON" : "OFF")}",
                "START_PUMP" => SetPumpState(true),
                "STOP_PUMP" => SetPumpState(false),
                "SYSTEM_STATUS" => $"STATUS -> Temp: {currentTemperature}C, Press: {currentPressure}BAR, Pump: {(isPumpRunning ? "RUNNING" : "STOPPED")}",
                _ => "ERROR: Unknown Command"
            };

            byte[] response = Encoding.UTF8.GetBytes(responseData);
            await stream.WriteAsync(response, 0, response.Length);
        }
    }

    private static string SetPumpState(bool state)
    {
        isPumpRunning = state;
        string statusText = isPumpRunning ? "[green]AVVIATA[/]" : "[red]ARRESTATA[/]";
        AnsiConsole.MarkupLine($"[bold blue]Comando Attuatore:[/] Pompa {statusText}");
        return $"SUCCESS: Pump is now {(isPumpRunning ? "RUNNING" : "STOPPED")}";
    }

    static async Task RunClient()
    {
        AnsiConsole.MarkupLine("[bold blue]CLIENT/MONITOR di Supervisione (HMI Terminal)[/]");
        
        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);

        using NetworkStream stream = client.GetStream();
        
        // Menu interattivo esteso con comandi di lettura e scrittura (attuatori)
        var command = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Seleziona il comando di controllo o lettura:")
                .PageSize(10)
                .AddChoices(new[] {
                    "READ_PRESSURE",
                    "READ_TEMP",
                    "START_PUMP",
                    "STOP_PUMP",
                    "SYSTEM_STATUS"
                }));
        
        byte[] commandBytes = Encoding.UTF8.GetBytes(command);
        await stream.WriteAsync(commandBytes, 0, commandBytes.Length);

        byte[] buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        // Rendering della risposta in tabella formattata
        var table = new Table();
        table.AddColumn("Parametro / Stato");
        table.AddColumn("Valore");
        table.AddRow("Comando inviato", command);
        table.AddRow("Risposta PLC", $"[bold]{message}[/]");
        
        AnsiConsole.Write(table);
    }
}