using System.Threading.Tasks;
using PlcBridge.Core.Models;

namespace PlcBridge.Core.Interfaces;

/// <summary>
/// Contratto enterprise che definisce le operazioni del PLC.
/// Disaccoppia la logica di business dall'implementazione hardware specifica.
/// </summary>
public interface IPlcService
{
    Task<bool> StartPumpAsync();
    Task<bool> StopPumpAsync();
    
    Task<double> ReadPressureAsync();
    Task<double> ReadTemperatureAsync();
    
    Task<PlcSystemStatus> GetSystemStatusAsync();
}