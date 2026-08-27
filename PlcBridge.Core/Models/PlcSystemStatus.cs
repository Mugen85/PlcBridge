namespace PlcBridge.Core.Models;

/// <summary>
/// Rappresenta lo stato istantaneo letto dal PLC.
/// Utilizziamo un 'record' per garantire l'immutabilità dei dati (DTOs).
/// </summary>
public record PlcSystemStatus(
    double Temperature, 
    double Pressure, 
    bool IsPumpRunning
);