using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PlcBridge.Core.Interfaces;
using PlcBridge.Infrastructure.Services;
using Xunit;

namespace PlcBridge.Tests;

public class PlcBridgeTests
{
    private readonly IPlcService _plcService;

    public PlcBridgeTests()
    {
        _plcService = new SimulatedPlcService(NullLogger<SimulatedPlcService>.Instance);
    }

    [Fact]
    public async Task GetSystemStatusAsync_ShouldReturnValidInitialStatus()
    {
        // Act
        var status = await _plcService.GetSystemStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.InRange(status.Temperature, 15.0, 85.0);
        Assert.InRange(status.Pressure, 8.0, 16.0);
    }

    [Fact]
    public async Task StartPumpAsync_ShouldSetIsPumpRunningToTrue()
    {
        // Act (chiamata asincrona diretta senza assegnazione a variabile)
        await _plcService.StartPumpAsync();
        var status = await _plcService.GetSystemStatusAsync();

        // Assert
        Assert.True(status.IsPumpRunning, "La pompa deve risultare accesa dopo StartPumpAsync.");
    }

    [Fact]
    public async Task StopPumpAsync_ShouldSetIsPumpRunningToFalse()
    {
        // Arrange
        await _plcService.StartPumpAsync();

        // Act
        await _plcService.StopPumpAsync();
        var status = await _plcService.GetSystemStatusAsync();

        // Assert
        Assert.False(status.IsPumpRunning, "La pompa deve risultare spenta dopo StopPumpAsync.");
    }

    [Fact]
    public async Task ReadSensors_ShouldReturnSensibleRanges()
    {
        // Act
        double temp = await _plcService.ReadTemperatureAsync();
        double press = await _plcService.ReadPressureAsync();

        // Assert
        Assert.InRange(temp, 15.0, 85.0);
        Assert.InRange(press, 8.0, 16.0);
    }
}