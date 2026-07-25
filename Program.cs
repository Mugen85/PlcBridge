using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

public interface IPlcController
{
    string StartPump();
    string StopPump();
    string ReadPressure();
    string ReadTemp();
    string GetSystemStatus();
}

public class PlcController : IPlcController
{
    private readonly Random _random = new Random();
    private bool _isPumpRunning = false;
    private double _currentPressure = 12.0;
    private double _currentTemperature = 22.0;

    public string StartPump()
    {
        _isPumpRunning = true;
        return "SUCCESS: Pump is now RUNNING";
    }

    public string StopPump()
    {
        _isPumpRunning = false;
        return "SUCCESS: Pump is now STOPPED";
    }

    public string ReadPressure()
    {
        _currentPressure = Math.Round(10.0 + (_random.NextDouble() * 5.0), 1);
        return $"PRESSURE: {_currentPressure} BAR | PUMP: {(_isPumpRunning ? "ON" : "OFF")}";
    }

    public string ReadTemp()
    {
        _currentTemperature = Math.Round(20.0 + (_random.NextDouble() * 10.0), 1);
        return $"TEMP: {_currentTemperature} C | PUMP: {(_isPumpRunning ? "ON" : "OFF")}";
    }

    public string GetSystemStatus()
    {
        return $"STATUS -> Temp: {_currentTemperature}C, Press: {_currentPressure}BAR, Pump: {(_isPumpRunning ? "RUNNING" : "STOPPED")}";
    }
}

class Program
{
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
            // Dependency Injection: Passando l'implementazione al Server
            IPlcController plc = new PlcController();
            await RunServer(plc);
        }
        else if (mode == "client")
        {
            await RunClient();
        }
    }

    static async Task RunServer(IPlcController plc)
    {
        AnsiConsole.MarkupLine("[bold green]PLC/SERVER Virtuale attivo sulla porta 5000 (Architecture Refactored)...[/]");
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

            // Delegating execution to the controller (Inversion of Control)
            string responseData = command switch
            {
                "READ_PRESSURE" => plc.ReadPressure(),
                "READ_TEMP" => plc.ReadTemp(),
                "START_PUMP" => plc.StartPump(),
                "STOP_PUMP" => plc.StopPump(),
                "SYSTEM_STATUS" => plc.GetSystemStatus(),
                _ => "ERROR: Unknown Command"
            };

            byte[] response = Encoding.UTF8.GetBytes(responseData);
            await stream.WriteAsync(response, 0, response.Length);
        }
    }

    static async Task RunClient()
    {
        AnsiConsole.MarkupLine("[bold blue]CLIENT/MONITOR di Supervisione (HMI Terminal)[/]");
        
        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);

        using NetworkStream stream = client.GetStream();
        
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

        var table = new Table();
        table.AddColumn("Parametro / Stato");
        table.AddColumn("Valore");
        table.AddRow("Comando inviato", command);
        table.AddRow("Risposta PLC", $"[bold]{message}[/]");
        
        AnsiConsole.Write(table);
    }
}