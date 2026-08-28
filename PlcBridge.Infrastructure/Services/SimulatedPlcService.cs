using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;

namespace PlcBridge.Infrastructure.Services;

/// <summary>
/// Implementazione concreta thread-safe per simulare il comportamento di un PLC.
/// </summary>
public class SimulatedPlcService : IPlcService
{
    private readonly ILogger<SimulatedPlcService> _logger;
    private readonly object _syncLock = new();

    private double _temperature = 22.0;
    private double _pressure = 12.0;
    private bool _isPumpRunning = false;

    public SimulatedPlcService(ILogger<SimulatedPlcService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlcSystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Simula latenza di lettura bus di campo (20ms)
        await Task.Delay(20, cancellationToken);

        lock (_syncLock)
        {
            UpdateSimulatedValues();
            return new PlcSystemStatus(_temperature, _pressure, _isPumpRunning);
        }
    }

    public async Task<double> ReadTemperatureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10, cancellationToken);

        lock (_syncLock)
        {
            UpdateSimulatedValues();
            return _temperature;
        }
    }

    public async Task<double> ReadPressureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10, cancellationToken);

        lock (_syncLock)
        {
            UpdateSimulatedValues();
            return _pressure;
        }
    }

    public async Task StartPumpAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(50, cancellationToken); // Simula tempo di attuazione relè

        lock (_syncLock)
        {
            _isPumpRunning = true;
            _logger.LogInformation("Stato PLC modificato: Pompa AVVIATA.");
        }
    }

    public async Task StopPumpAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(50, cancellationToken);

        lock (_syncLock)
        {
            _isPumpRunning = false;
            _logger.LogInformation("Stato PLC modificato: Pompa ARRESTATA.");
        }
    }

    private void UpdateSimulatedValues()
    {
        // Variazione deterministica con rumore controllato
        double tempDelta = (Random.Shared.NextDouble() * 1.0) - 0.5;
        double pressDelta = (Random.Shared.NextDouble() * 0.4) - 0.2;

        _temperature = Math.Clamp(Math.Round(_temperature + tempDelta, 1), 15.0, 85.0);
        _pressure = Math.Clamp(Math.Round(_pressure + pressDelta, 1), 8.0, 16.0);
    }
}