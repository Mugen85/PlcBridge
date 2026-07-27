using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;

namespace PlcBridge.Worker.Services;

/// <summary>
/// Questo servizio gira in background indipendentemente dalla UI.
/// Il suo scopo è mantenere viva la connessione col PLC e leggere i dati.
/// </summary>
public class PlcPollingWorker : BackgroundService
{
    private readonly IPlcDriver _plcDriver;
    private readonly ILogger<PlcPollingWorker> _logger;

    // Il motore di Dependency Injection ci inietta in automatico il Driver e il Logger
    public PlcPollingWorker(IPlcDriver plcDriver, ILogger<PlcPollingWorker> logger)
    {
        _plcDriver = plcDriver;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Avvio del Polling Worker in background...");

        // Continua a girare finché non viene richiesta la chiusura dell'app
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Gestione resiliente della connessione
                if (_plcDriver.State != ConnectionState.Connected)
                {
                    _logger.LogWarning("PLC non connesso. Tentativo di connessione in corso...");
                    await _plcDriver.ConnectAsync(stoppingToken);
                    _logger.LogInformation("Connessione al PLC stabilita con successo.");
                }

                // Polling vero e proprio (per ora leggiamo la pressione a scopo di test)
                var pressure = await _plcDriver.ReadTagAsync("PRESSURE", stoppingToken);
                _logger.LogDebug("Dato dal campo -> PRESSURE: {Value} Bar", pressure);

                // Pausa industriale (es. ciclo di lettura ogni 500ms)
                await Task.Delay(500, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Errore di comunicazione nel ciclo di polling. Riprovo...");
                
                // Backoff: se c'è un errore, aspettiamo un po' prima di intasare la rete di tentativi
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("Arresto del Polling Worker. Chiusura connessione PLC...");
        await _plcDriver.DisconnectAsync();
    }
}