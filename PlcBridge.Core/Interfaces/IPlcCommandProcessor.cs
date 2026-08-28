using System.Threading;
using System.Threading.Tasks;

namespace PlcBridge.Core.Interfaces;

/// <summary>
/// Contratto per il parsing e l'elaborazione dei comandi testuali ricevuti dal bridge.
/// Disaccoppia il protocollo di rete (TCP, MQTT, HTTP) dalla logica applicativa.
/// </summary>
public interface IPlcCommandProcessor
{
    /// <summary>
    /// Elabora un comando di testo in ingresso e restituisce la risposta formattata per il client.
    /// </summary>
    Task<string> ProcessCommandAsync(string rawCommand, CancellationToken cancellationToken);
}