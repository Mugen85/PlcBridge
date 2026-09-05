using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options; // <-- Assicurati di avere questo using
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;
using PlcBridge.Infrastructure.Services;
using Xunit;

namespace PlcBridge.Tests;

public class TcpServerIntegrationTests : IAsyncDisposable
{
    private readonly int _testPort = 50505; // Porta isolata per i test
    private CancellationTokenSource? _cts;
    private TcpPlcServer? _server;
    private IPlcService? _plcService;
    private IPlcCommandProcessor? _processor;

    public TcpServerIntegrationTests()
    {
        _plcService = new SimulatedPlcService(NullLogger<SimulatedPlcService>.Instance);
        _processor = new PlcCommandProcessor(_plcService, NullLogger<PlcCommandProcessor>.Instance);
        
        // Creiamo le opzioni mockate per il test usando IOptions
        var options = Options.Create(new TcpSettings 
        { 
            BindAddress = "127.0.0.1", 
            Port = _testPort 
        });

        _cts = new CancellationTokenSource();
        _server = new TcpPlcServer(_processor, NullLogger<TcpPlcServer>.Instance, options);

        // Avvia il server TCP in background in modo non bloccante
        _ = _server.StartAsync(_cts.Token);
    }

    [Fact]
    public async Task TcpServer_ShouldProcessCommandAndReturnResponse()
    {
        // Attesa tecnica minima per l'avvio del listener di rete
        await Task.Delay(100);

        // Act: Connessione client reale su loopback
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", _testPort);

        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        using var reader = new StreamReader(stream, Encoding.UTF8);

        await writer.WriteLineAsync("START_PUMP");
        string? response = await reader.ReadLineAsync();

        // Assert
        Assert.Equal("OK:PUMP_STARTED", response);
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
        if (_server != null)
        {
            // Arresto controllato
            await Task.Delay(50);
        }
    }
}