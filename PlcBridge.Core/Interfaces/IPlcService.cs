using System.Threading;
using System.Threading.Tasks;
using PlcBridge.Core.Models;

namespace PlcBridge.Core.Interfaces;

/// <summary>
/// Contratto di dominio per la gestione e il controllo del PLC.
/// Ogni operazione asincrona supporta la cancellazione cooperativa tramite CancellationToken.
/// </summary>
public interface IPlcService
{
    Task<PlcSystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken = default);
    Task<double> ReadTemperatureAsync(CancellationToken cancellationToken = default);
    Task<double> ReadPressureAsync(CancellationToken cancellationToken = default);
    Task StartPumpAsync(CancellationToken cancellationToken = default);
    Task StopPumpAsync(CancellationToken cancellationToken = default);
}