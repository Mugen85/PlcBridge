using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;

namespace PlcBridge.Infrastructure.Services;

public class TcpPlcServer : BackgroundService
{
    private readonly IPlcCommandProcessor _commandProcessor;
    private readonly ILogger<TcpPlcServer> _logger;
    private readonly TcpSettings _settings;
    private TcpListener? _listener;

    public TcpPlcServer(
        IPlcCommandProcessor commandProcessor, 
        ILogger<TcpPlcServer> logger, 
        IOptions<TcpSettings> settings)
    {
        _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Converte la stringa di configurazione (es. "127.0.0.1" o "0.0.0.0") in un oggetto IPAddress
        if (!IPAddress.TryParse(_settings.BindAddress, out var ipAddress))
        {
            ipAddress = IPAddress.Loopback;
            _logger.LogWarning("BindAddress non valido ({ConfigAddress}). Utilizzo di fallback: 127.0.0.1", _settings.BindAddress);
        }

        _listener = new TcpListener(ipAddress, _settings.Port);
        _listener.Start();

        _logger.LogInformation("TCP Server avviato in ascolto su {Ip}:{Port}...", ipAddress, _settings.Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(stoppingToken);
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

    // (Il resto dei metodi HandleClientAsync rimane invariato)
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        // ... (lasciare identico a prima)
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
                    string? rawCommand = await reader.ReadLineAsync(cancellationToken);
                    if (rawCommand is null) break;

                    string response = await _commandProcessor.ProcessCommandAsync(rawCommand, cancellationToken);
                    await writer.WriteLineAsync(response.AsMemory(), cancellationToken);
                }
            }
            catch (Exception)
            {
                // Gestione eccezioni socket/chiusura
            }
        }
    }
}