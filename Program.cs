using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;
using Serilog;

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
        // Configurazione Serilog con Log Rotation
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/plcbridge-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 3 // Mantiene solo gli ultimi 3 file
            )
            .CreateLogger();

        try
        {
            if (args.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]Errore:[/ red] Specificare [yellow]server[/] o [yellow]client[/].");
                return;
            }

            string mode = args[0].ToLower();

            if (mode == "server")
            {
                IPlcController plc = new PlcController();
                await RunServer(plc);
            }
            else if (mode == "client")
            {
                await RunClient();
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Il programma è terminato in modo imprevisto.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static async Task RunServer(IPlcController plc)
    {
        Log.Information("PLC/SERVER Virtuale avviato sulla porta 5000.");
        AnsiConsole.MarkupLine("[bold green]PLC/SERVER Virtuale attivo sulla porta 5000...[/]");

        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();

        while (true)
        {
            using TcpClient client = await listener.AcceptTcpClientAsync();
            using NetworkStream stream = client.GetStream();

            Log.Information("Client connesso: {Endpoint}", client.Client.RemoteEndPoint);

            try
            {
                while (client.Connected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        // Il client ha chiuso la connessione in modo pulito
                        Log.Information("Client disconnesso.");
                        break;
                    }

                    string command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    Log.Information("Richiesta ricevuta: {Command}", command);
                    AnsiConsole.MarkupLine($"[dim]Richiesta ricevuta:[/] [yellow]{command}[/]");

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
            catch (IOException ex)
            {
                // Il client si è disconnesso in modo brusco (es. Ctrl+C, crash)
                Log.Warning(ex, "Connessione interrotta dal client.");
            }
        }
    }
    static async Task RunClient()
    {
        Log.Information("Avvio del client di supervisione.");
        AnsiConsole.MarkupLine("[bold blue]CLIENT/MONITOR di Supervisione (HMI Terminal)[/]");

        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);

        using NetworkStream stream = client.GetStream();

        const string exitChoice = "ESCI";

        while (true)
        {
            var command = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Seleziona il comando di controllo o lettura:")
                    .PageSize(10)
                    .AddChoices(new[] {
                "READ_PRESSURE",
                "READ_TEMP",
                "START_PUMP",
                "STOP_PUMP",
                "SYSTEM_STATUS",
                exitChoice
                    }));

            if (command == exitChoice)
            {
                Log.Information("Client terminato dall'utente.");
                break;
            }

            Log.Information("Comando inviato al server: {Command}", command);

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
}
