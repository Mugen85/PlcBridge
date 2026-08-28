using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlcBridge.Core.Interfaces;

namespace PlcBridge.Infrastructure.Services;

/// <summary>
/// Server TCP industriale multi-client e non bloccante.
/// Eredita da BackgroundService per essere gestito direttamente dal ciclo di vita dell'Host .NET.
/// </summary>
public class TcpPlcServer : BackgroundService
{
    private readonly IPlcCommandProcessor _commandProcessor;
    private readonly ILogger<TcpPlcServer> _logger;
    private readonly int _port;
    private TcpListener? _listener;

    public TcpPlcServer(
        IPlcCommandProcessor commandProcessor, 
        ILogger<TcpPlcServer> logger, 
        int port = 5000)
    {
        _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _port = port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ascoltiamo su qualsiasi interfaccia di rete (Loopback + LAN)
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        _logger.LogInformation("TCP Server avviato in ascolto sulla porta {Port}...", _port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Attende la connessione di un client in modo asincrono
                TcpClient client = await _listener.AcceptTcpClientAsync(stoppingToken);

                // AVVIO TASK DEDICATO (Fire-and-Forget controllato):
                // Non facciamo l'await di HandleClientAsync per non bloccare l'ascolto di nuovi client!
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Arresto del server TCP richiesto (Shutdown).");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Errore irreversibile nel listener TCP.");
        }
        finally
        {
            _listener.Stop();
            _logger.LogInformation("TCP Listener arrestato.");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        _logger.LogInformation("Nuovo client collegato: {EndPoint}", clientEndpoint);

        using (client)
        using (NetworkStream stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Lettura riga per riga (fino al terminatore \n)
                    string? rawCommand = await reader.ReadLineAsync(cancellationToken);

                    // Se readLineAsync restituisce null, il client ha chiuso la connessione
                    if (rawCommand is null)
                    {
                        _logger.LogInformation("Client {EndPoint} ha chiuso la connessione.", clientEndpoint);
                        break;
                    }

                    _logger.LogDebug("Ricevuto da {EndPoint}: '{Command}'", clientEndpoint, rawCommand);

                    // Elaboriamo il comando tramite il CommandProcessor disaccoppiato
                    string response = await _commandProcessor.ProcessCommandAsync(rawCommand, cancellationToken);

                    // Inviamo la risposta al client con terminatore di riga
                    await writer.WriteLineAsync(response.AsMemory(), cancellationToken);
                    _logger.LogDebug("Inviato a {EndPoint}: '{Response}'", clientEndpoint, response);
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                _logger.LogWarning("Connessione interrotta bruscamente dal client {EndPoint}.", clientEndpoint);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Chiusura forzata della connessione {EndPoint} per spegnimento host.", clientEndpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore imprevisto nella gestione del client {EndPoint}.", clientEndpoint);
            }
        }

        _logger.LogInformation("Risorse del client {EndPoint} rilasciate con successo.", clientEndpoint);
    }
}