namespace PlcBridge.Core.Models;

/// <summary>
/// Opzioni di configurazione per i socket TCP (Server e Client).
/// </summary>
public class TcpSettings
{
    public const string SectionName = "TcpSettings";

    /// <summary>
    /// Indirizzo IP su cui il server si mette in ascolto (es. "127.0.0.1" o "0.0.0.0" per LAN/Any).
    /// </summary>
    public string BindAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Host di destinazione a cui il client (WebHmi) si connette.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// Porta TCP di comunicazione.
    /// </summary>
    public int Port { get; set; } = 5050;
}