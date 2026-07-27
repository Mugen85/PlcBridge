using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;

namespace PlcBridge.Infrastructure.Drivers;

/// <summary>
/// Un driver finto che implementa IPlcDriver per simulare il comportamento di un PLC.
/// Utile per sviluppare e testare la logica e la UI senza hardware fisico.
/// </summary>
public class SimulatorPlcDriver : IPlcDriver
{
    private ConnectionState _state = ConnectionState.Disconnected;
    
    // Espone lo stato corrente richiesto dall'interfaccia
    public ConnectionState State => _state;

    // Usiamo un dizionario per simulare i registri di memoria del PLC
    private readonly Dictionary<string, object> _memory = new();

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _state = ConnectionState.Connecting;
        
        // Simuliamo un ritardo di rete (es. handshake TCP)
        await Task.Delay(1000, cancellationToken);
        
        // Inizializziamo lo stato della nostra "pompa" simulata
        _memory["PUMP_STATUS"] = false;
        
        _state = ConnectionState.Connected;
    }

    public Task DisconnectAsync()
    {
        _state = ConnectionState.Disconnected;
        return Task.CompletedTask; // Operazione sincrona mascherata da asincrona
    }

    public async Task<object> ReadTagAsync(string address, CancellationToken cancellationToken)
    {
        // Se qualcuno cerca di leggere ma non siamo connessi, lanciamo eccezione
        if (_state != ConnectionState.Connected)
        {
            throw new InvalidOperationException("Impossibile leggere: il PLC non è connesso.");
        }

        // Simuliamo la latenza di lettura dal campo (es. 50ms)
        await Task.Delay(50, cancellationToken);

        // Simulazione della generazione dei dati in base all'indirizzo richiesto
        if (address == "PRESSURE")
        {
            return Math.Round(10.0 + (Random.Shared.NextDouble() * 5.0), 1);
        }
        
        if (address == "TEMP")
        {
            return Math.Round(20.0 + (Random.Shared.NextDouble() * 10.0), 1);
        }

        // Per la pompa, leggiamo dalla nostra "memoria" interna
        if (_memory.TryGetValue(address, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"L'indirizzo {address} non esiste nel PLC.");
    }

    public async Task WriteTagAsync(string address, object value, CancellationToken cancellationToken)
    {
        if (_state != ConnectionState.Connected)
        {
            throw new InvalidOperationException("Impossibile scrivere: il PLC non è connesso.");
        }

        // Simuliamo la latenza di scrittura
        await Task.Delay(50, cancellationToken);

        // Scriviamo nella memoria simulata
        _memory[address] = value;
    }
}