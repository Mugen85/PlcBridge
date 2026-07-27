using System;

namespace PlcBridge.Core.Models;

/// <summary>
/// Rappresenta un singolo dato (sensore, motore, stato) letto o scritto sul PLC.
/// Gestisce internamente la concorrenza (Thread-Safety).
/// </summary>
public class PlcTag
{
    public string Name { get; }
    public string Address { get; }

    // Oggetto usato esclusivamente per sincronizzare l'accesso ai dati in multithreading
    private readonly object _syncLock = new object();

    private object _value = default!;
    public object Value 
    { 
        get { lock (_syncLock) return _value; }
    }

    private DateTime _lastUpdated;
    public DateTime LastUpdated 
    { 
        get { lock (_syncLock) return _lastUpdated; }
    }

    // La "Quality" è un concetto industriale fondamentale: il sensore risponde, 
    // ma il dato è affidabile (es. non è fuori scala)?
    private bool _isQualityGood;
    public bool IsQualityGood 
    { 
        get { lock (_syncLock) return _isQualityGood; }
    }

    public PlcTag(string name, string address)
    {
        Name = name;
        Address = address;
        _lastUpdated = DateTime.MinValue;
    }

    /// <summary>
    /// Aggiorna il tag in modo atomico. 
    /// UI e Worker non andranno mai in collisione.
    /// </summary>
    public void UpdateValue(object newValue, bool isQualityGood)
    {
        lock (_syncLock)
        {
            _value = newValue;
            _isQualityGood = isQualityGood;
            _lastUpdated = DateTime.UtcNow;
        }
    }
}