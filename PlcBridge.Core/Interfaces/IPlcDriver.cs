using System.Threading;
using System.Threading.Tasks;
using PlcBridge.Core.Models;

namespace PlcBridge.Core.Interfaces;

/// <summary>
/// Interfaccia che astrae il concetto di PLC. 
/// Il Core non sa se sotto c'è un Siemens, un Modbus, un Allen-Bradley o un Simulatore.
/// </summary>
public interface IPlcDriver
{
    ConnectionState State { get; }
    
    Task ConnectAsync(CancellationToken cancellationToken);
    
    Task DisconnectAsync();
    
    // Metodi CRUD verso il PLC
    Task<object> ReadTagAsync(string address, CancellationToken cancellationToken);
    Task WriteTagAsync(string address, object value, CancellationToken cancellationToken);
}