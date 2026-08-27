using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Spectre.Console;
using PlcBridge.Core.Interfaces;
using PlcBridge.Infrastructure.Services;
using PlcBridge.Worker.Services;

namespace PlcBridge.Worker;

/// <summary>
/// Punto d'ingresso principale del Worker Host.
/// Configura il logging strutturato, il container di Dependency Injection e il ciclo di vita (Graceful Shutdown).
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/plcbridge-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
            .CreateLogger();

        try
        {
            Log.Information("Bootstrapping PlcBridge Worker Host...");

            // Configurazione dell'Host .NET Core con Inversion of Control e DI
            using IHost host = Host.CreateDefaultBuilder(args)
                .UseSerilog() // Integrazione Serilog come logging provider principale
                .ConfigureServices((hostContext, services) =>
                {
                    // Collega il contratto di dominio IPlcService all'implementazione concreta SimulatedPlcService (Singleton)
                    services.AddSingleton<IPlcService, SimulatedPlcService>();
                    
                    // Registra il servizio di background per il polling industriale
                    services.AddHostedService<PlcPollingWorker>();
                })
                .Build();

            // Avvio asincrono dell'Host (non bloccante per il thread UI)
            await host.StartAsync();

            AnsiConsole.MarkupLine("\n[bold green]PlcBridge Engine avviato con successo![/]");
            AnsiConsole.MarkupLine("Premi [yellow]ESC[/] per arrestare il servizio in modo sicuro (Graceful Shutdown).\n");

            // Loop della Console UI per intercettare l'uscita pulita
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
            Log.Fatal(ex, "Il programma è terminato a causa di un'eccezione non gestita.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}