using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PlcBridge.Core.Interfaces;
using PlcBridge.Infrastructure.Services;
using Xunit;

namespace PlcBridge.Tests;

public class PlcCommandProcessorTests
{
    private readonly IPlcService _plcService;
    private readonly IPlcCommandProcessor _processor;

    public PlcCommandProcessorTests()
    {
        _plcService = new SimulatedPlcService(NullLogger<SimulatedPlcService>.Instance);
        _processor = new PlcCommandProcessor(_plcService, NullLogger<PlcCommandProcessor>.Instance);
    }

    [Theory]
    [InlineData("START_PUMP", "OK:PUMP_STARTED")]
    [InlineData("STOP_PUMP", "OK:PUMP_STOPPED")]
    [InlineData("INVALID_CMD", "ERR:UNKNOWN_COMMAND:INVALID_CMD")]
    [InlineData("", "ERR:EMPTY_COMMAND")]
    [InlineData("   ", "ERR:EMPTY_COMMAND")]
    public async Task ProcessCommandAsync_ShouldReturnExpectedFormat(string command, string expectedPrefix)
    {
        // Act
        var response = await _processor.ProcessCommandAsync(command, CancellationToken.None);

        // Assert
        Assert.StartsWith(expectedPrefix, response);
    }

    [Fact]
    public async Task ProcessCommandAsync_GetStatus_ShouldReturnStructuredFormat()
    {
        // Act
        var response = await _processor.ProcessCommandAsync("GET_STATUS", CancellationToken.None);

        // Assert: verifichiamo la presenza dei campi previsti nel protocollo
        Assert.StartsWith("OK:", response);
        Assert.Contains("TEMP=", response);
        Assert.Contains("PRESS=", response);
        Assert.Contains("PUMP=", response);
    }
}