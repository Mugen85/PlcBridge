using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;

namespace PlcBridge.WebHmi.Services;

/// <summary>
/// Implementazione di IPlcService per la Web HMI che inoltra le chiamate 
/// al PlcBridge.Worker remoto/locale tramite protocollo TCP/IP.
/// </summary>
public class NetworkPlcService : IPlcService, IAsyncDisposable
{
    const string Host = "127.0.0.1";
    const int Port = 5050;

    readonly ILogger<NetworkPlcService> _logger;
    TcpClient? _client;
    NetworkStream? _stream;
    StreamReader? _reader;
    StreamWriter? _writer;
    readonly SemaphoreSlim _lock = new(1, 1);

    public NetworkPlcService(ILogger<NetworkPlcService> logger)
    {
        _logger = logger;
    }

    async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client is { Connected: true } && _stream != null && _reader != null && _writer != null)
            return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Doppio controllo in thread safety
            if (_client is { Connected: true }) return;

            _logger.LogInformation("Tentativo di connessione TCP al bridge {Host}:{Port}...", Host, Port);

            _client = new TcpClient();
            await _client.ConnectAsync(Host, Port, cancellationToken);

            _stream = _client.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);
            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

            _logger.LogInformation("Connessione TCP al bridge stabilita con successo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impossibile connettersi al server TCP del PLC.");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task<string> SendAndReceiveAsync(string command, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_writer is null || _reader is null)
                throw new InvalidOperationException("Canale di comunicazione TCP non inizializzato.");

            // Invio comando al server
            await _writer.WriteLineAsync(command.AsMemory(), cancellationToken);

            // Lettura risposta dal server
            string? response = await _reader.ReadLineAsync(cancellationToken);

            if (response is null)
                throw new IOException("Il server TCP ha chiuso la connessione inaspettatamente.");

            return response;
        }
        catch (Exception)
        {
            // Se c'è un errore di comunicazione, resettiamo il client per forzare la riconnessione al giro successivo
            await ResetConnectionAsync();
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    Task ResetConnectionAsync()
    {
        try
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();
            _client?.Dispose();
        }
        catch { /* Ignora errori di pulizia socket */ }

        _client = null;
        _stream = null;
        _reader = null;
        _writer = null;
        return Task.CompletedTask;
    }

    public async Task<PlcSystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        // Protocollo: GET_STATUS -> Risposta attesa: OK:TEMP=24.5;PRESS=12.1;PUMP=1
        string rawResponse = await SendAndReceiveAsync("GET_STATUS", cancellationToken);

        if (!rawResponse.StartsWith("OK:"))
            throw new InvalidOperationException($"Risposta non valida dal PLC: {rawResponse}");

        string payload = rawResponse.Substring(3); // Rimuove "OK:"
        double temp = 0;
        double press = 0;
        bool pump = false;

        foreach (var part in payload.Split(';'))
        {
            var kv = part.Split('=');
            if (kv.Length != 2) continue;

            if (kv[0] == "TEMP") double.TryParse(kv[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out temp);
            if (kv[0] == "PRESS") double.TryParse(kv[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out press);
            if (kv[0] == "PUMP") pump = kv[1] == "1";
        }

        return new PlcSystemStatus(temp, press, pump);
    }

    public async Task<double> ReadTemperatureAsync(CancellationToken cancellationToken = default)
    {
        string rawResponse = await SendAndReceiveAsync("READ_TEMP", cancellationToken);
        if (rawResponse.StartsWith("OK:") && double.TryParse(rawResponse.Substring(3), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
            return val;

        throw new InvalidOperationException($"Errore lettura temperatura: {rawResponse}");
    }

    public async Task<double> ReadPressureAsync(CancellationToken cancellationToken = default)
    {
        string rawResponse = await SendAndReceiveAsync("READ_PRESSURE", cancellationToken);

        if (rawResponse.StartsWith("OK:") && double.TryParse(rawResponse.Substring(3), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pressureVal))
        {
            return pressureVal;
        }

        throw new InvalidOperationException($"Errore lettura pressione: {rawResponse}");
    }

    public async Task StartPumpAsync(CancellationToken cancellationToken = default)
    {
        string rawResponse = await SendAndReceiveAsync("START_PUMP", cancellationToken);
        if (!rawResponse.StartsWith("OK"))
            throw new InvalidOperationException($"Impossibile avviare la pompa: {rawResponse}");
    }

    public async Task StopPumpAsync(CancellationToken cancellationToken = default)
    {
        string rawResponse = await SendAndReceiveAsync("STOP_PUMP", cancellationToken);
        if (!rawResponse.StartsWith("OK"))
            throw new InvalidOperationException($"Impossibile arrestare la pompa: {rawResponse}");
    }

    public async ValueTask DisposeAsync()
    {
        await ResetConnectionAsync();
        _lock.Dispose();
    }
}