namespace PlcBridge.Core.Models;

/// <summary>
/// Rappresenta in modo esplicito lo stato della connessione verso il campo (PLC).
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted
}