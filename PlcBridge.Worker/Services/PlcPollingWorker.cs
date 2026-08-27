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
/// Il suo scopo è effettuare il polling periodico dei dati del PLC tramite IPlcService.
/// </summary>
public class PlcPollingWorker : BackgroundService
{
    private readonly IPlcService _plcService;
    private readonly ILogger<PlcPollingWorker> _logger;

    // Il motore di Dependency Injection ci inietta in automatico il servizio PLC e il Logger
    public PlcPollingWorker(IPlcService plcService, ILogger<PlcPollingWorker> logger)
    {
        _plcService = plcService;
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
                // Polling industriale dello stato di sistema tramite l'interfaccia di dominio
                var status = await _plcService.GetSystemStatusAsync();
                
                _logger.LogDebug(
                    "Polling dal campo -> Temp: {Temp}°C | Pressione: {Pressure} BAR | Pompa: {IsRunning}", 
                    status.Temperature, 
                    status.Pressure, 
                    status.IsPumpRunning
                );

                // Pausa industriale (es. ciclo di lettura ogni 1000ms)
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Errore di comunicazione nel ciclo di polling in background. Riprovo...");
                
                // Backoff in caso di errore di comunicazione
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("Arresto del Polling Worker completato.");
    }
}