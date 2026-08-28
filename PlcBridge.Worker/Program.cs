using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using PlcBridge.Core.Interfaces;
using PlcBridge.Infrastructure.Services;
using PlcBridge.Worker.Services;

namespace PlcBridge.Worker;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Configurazione del Logger globale di Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/plcbridge-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
            .CreateLogger();

        try
        {
            Log.Information("Bootstrapping PlcBridge Worker Host...");

            using IHost host = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    // Dominio e Logica Applicativa
                    services.AddSingleton<IPlcService, SimulatedPlcService>();
                    services.AddSingleton<IPlcCommandProcessor, PlcCommandProcessor>();

                    // Server TCP su porta 5050 (configurato esplicitamente tramite factory)
                    services.AddHostedService(sp => new TcpPlcServer(
                        sp.GetRequiredService<IPlcCommandProcessor>(),
                        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TcpPlcServer>>(),
                        port: 5050));

                    // Polling Worker in background
                    services.AddHostedService<PlcPollingWorker>();
                })
                .Build();

            await host.StartAsync();

            AnsiConsole.MarkupLine("\n[bold green]PlcBridge Engine & TCP Server (Porta 5050) avviati con successo![/]");
            AnsiConsole.MarkupLine("Premi [yellow]ESC[/] per arrestare il servizio in modo sicuro (Graceful Shutdown).\n");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }
                await Task.Delay(100);
            }

            Log.Information("Segnale di uscita ricevuto. Arresto controllato dei servizi in corso...");
            await host.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Il programma è terminato inaspettatamente.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}