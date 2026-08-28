using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlcBridge.Core.Interfaces;

namespace PlcBridge.Infrastructure.Services;

public class PlcCommandProcessor : IPlcCommandProcessor
{
    private readonly IPlcService _plcService;
    private readonly ILogger<PlcCommandProcessor> _logger;

    public PlcCommandProcessor(IPlcService plcService, ILogger<PlcCommandProcessor> logger)
    {
        _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ProcessCommandAsync(string rawCommand, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawCommand))
        {
            return "ERR:EMPTY_COMMAND";
        }

        string command = rawCommand.Trim().ToUpperInvariant();
        _logger.LogDebug("Elaborazione comando protocollo: {Command}", command);

        try
        {
            return command switch
            {
                "GET_STATUS" => await HandleGetStatusAsync(cancellationToken),
                "READ_PRESSURE" => await HandleReadPressureAsync(cancellationToken),
                "READ_TEMP" => await HandleReadTempAsync(cancellationToken),
                "START_PUMP" => await HandleStartPumpAsync(cancellationToken),
                "STOP_PUMP" => await HandleStopPumpAsync(cancellationToken),
                _ => $"ERR:UNKNOWN_COMMAND:{command}"
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Errore interno durante l'elaborazione del comando: {Command}", command);
            return "ERR:INTERNAL_SERVER_ERROR";
        }
    }

    private async Task<string> HandleGetStatusAsync(CancellationToken cancellationToken)
    {
        var status = await _plcService.GetSystemStatusAsync(cancellationToken);
        string pumpFlag = status.IsPumpRunning ? "1" : "0";
        return string.Format(
            CultureInfo.InvariantCulture,
            "OK:TEMP={0:F1};PRESS={1:F1};PUMP={2}",
            status.Temperature,
            status.Pressure,
            pumpFlag);
    }

    private async Task<string> HandleReadPressureAsync(CancellationToken cancellationToken)
    {
        double pressure = await _plcService.ReadPressureAsync(cancellationToken);
        return string.Format(CultureInfo.InvariantCulture, "OK:{0:F1}", pressure);
    }

    private async Task<string> HandleReadTempAsync(CancellationToken cancellationToken)
    {
        double temp = await _plcService.ReadTemperatureAsync(cancellationToken);
        return string.Format(CultureInfo.InvariantCulture, "OK:{0:F1}", temp);
    }

    private async Task<string> HandleStartPumpAsync(CancellationToken cancellationToken)
    {
        await _plcService.StartPumpAsync(cancellationToken);
        return "OK:PUMP_STARTED";
    }

    private async Task<string> HandleStopPumpAsync(CancellationToken cancellationToken)
    {
        await _plcService.StopPumpAsync(cancellationToken);
        return "OK:PUMP_STOPPED";
    }
}