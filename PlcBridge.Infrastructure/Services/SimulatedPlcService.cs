using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;

namespace PlcBridge.Infrastructure.Services;

/// <summary>
/// Implementazione concreta del PLC simulato per l'Infrastructure Layer.
/// Gestisce lo stato interno in modo thread-safe e adotta il logging strutturato.
/// </summary>
public class SimulatedPlcService : IPlcService
{
    private readonly ILogger<SimulatedPlcService> _logger;
    private readonly Random _random = new();
    private readonly object _lock = new();

    private bool _isPumpRunning = false;
    private double _currentPressure = 12.0;
    private double _currentTemperature = 22.0;

    public SimulatedPlcService(ILogger<SimulatedPlcService> logger)
    {
        _logger = logger;
    }

    public Task<bool> StartPumpAsync()
    {
        lock (_lock)
        {
            _isPumpRunning = true;
            _logger.LogInformation("Hardware attuatore: Pompa avviata con successo.");
        }
        return Task.FromResult(true);
    }

    public Task<bool> StopPumpAsync()
    {
        lock (_lock)
        {
            _isPumpRunning = false;
            _logger.LogInformation("Hardware attuatore: Pompa arrestata.");
        }
        return Task.FromResult(false);
    }

    public Task<double> ReadPressureAsync()
    {
        lock (_lock)
        {
            _currentPressure = Math.Round(10.0 + (_random.NextDouble() * 5.0), 1);
            _logger.LogDebug("Sensore pressione campionato: {Pressure} BAR", _currentPressure);
            return Task.FromResult(_currentPressure);
        }
    }

    public Task<double> ReadTemperatureAsync()
    {
        lock (_lock)
        {
            _currentTemperature = Math.Round(20.0 + (_random.NextDouble() * 10.0), 1);
            _logger.LogDebug("Sensore temperatura campionato: {Temp} C", _currentTemperature);
            return Task.FromResult(_currentTemperature);
        }
    }

    public Task<PlcSystemStatus> GetSystemStatusAsync()
    {
        lock (_lock)
        {
            _currentPressure = Math.Round(10.0 + (_random.NextDouble() * 5.0), 1);
            _currentTemperature = Math.Round(20.0 + (_random.NextDouble() * 10.0), 1);

            var status = new PlcSystemStatus(
                Temperature: _currentTemperature,
                Pressure: _currentPressure,
                IsPumpRunning: _isPumpRunning
            );

            _logger.LogDebug("Snapshot di sistema generato: {@Status}", status);
            return Task.FromResult(status);
        }
    }
}