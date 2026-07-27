// Sostituisci INTERAMENTE il tuo vecchio Program.cs con questo
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Spectre.Console;
using PlcBridge.Core.Interfaces;
using PlcBridge.Infrastructure.Drivers;
using PlcBridge.Worker.Services;

namespace PlcBridge.Worker;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Inizializziamo Serilog all'inizio per catturare anche eventuali crash di avvio
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/plcbridge-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
            .CreateLogger();

        try
        {
            Log.Information("Bootstrapping PlcBridge Worker...");

            // 2. Creiamo e configuriamo l'Host
            using IHost host = Host.CreateDefaultBuilder(args)
                .UseSerilog() // Diciamo all'Host di usare Serilog al posto del logger Microsoft base
                .ConfigureServices((hostContext, services) =>
                {
                    // DIPENDENZA: Quando qualcuno chiede un IPlcDriver, dagli il SimulatorPlcDriver (una sola istanza per tutti -> Singleton)
                    services.AddSingleton<IPlcDriver, SimulatorPlcDriver>();
                    
                    // WORKER: Registriamo il nostro BackgroundService
                    services.AddHostedService<PlcPollingWorker>();
                })
                .Build();

            // 3. Avviamo l'Host in modo asincrono. 
            // StartAsync non è bloccante: avvia i worker in background e restituisce il controllo.
            await host.StartAsync();

            // 4. UI Thread (Il thread principale è ora libero per occuparsi della UI)
            AnsiConsole.MarkupLine("\n[bold green]Sistema avviato con successo![/]");
            AnsiConsole.MarkupLine("Premi [yellow]ESC[/] per arrestare il servizio in modo pulito.\n");

            // Loop della UI che attende semplicemente la pressione del tasto ESC
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        break; // Usciamo dal loop UI
                    }
                }
                
                // Mettiamo in sleep il thread della UI per non consumare il 100% della CPU
                await Task.Delay(100); 
            }

            // 5. Shutdown pulito (Graceful Shutdown)
            Log.Information("Segnale di uscita (ESC) ricevuto. Spegnimento dei servizi in corso...");
            await host.StopAsync(); // Questo avvisa tutti i BackgroundService di fermarsi tramite il CancellationToken
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Il programma è terminato in modo imprevisto (Crash).");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}