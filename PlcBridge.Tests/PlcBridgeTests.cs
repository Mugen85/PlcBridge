using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;
using PlcBridge.Infrastructure.Services;
using Xunit;

namespace PlcBridge.Tests;

/// <summary>
/// Suite di test unitari xUnit per validare il comportamento del servizio PLC simulato.
/// Isola la logica di business utilizzando il NullLogger.
/// </summary>
public class PlcServiceTests
{
    private readonly IPlcService _plcService;

    public PlcServiceTests()
    {
        // Inseriamo il logger nullo per soddisfare le dipendenze del costruttore senza log su disco nei test
        var logger = NullLogger<SimulatedPlcService>.Instance;
        _plcService = new SimulatedPlcService(logger);
    }

    [Fact]
    public async Task StartPumpAsync_ShouldSetPumpRunningToTrue()
    {
        // Act
        var result = await _plcService.StartPumpAsync();
        var status = await _plcService.GetSystemStatusAsync();

        // Assert
        Assert.True(result);
        Assert.True(status.IsPumpRunning);
    }

    [Fact]
    public async Task StopPumpAsync_ShouldSetPumpRunningToFalse()
    {
        // Arrange
        await _plcService.StartPumpAsync();

        // Act
        var result = await _plcService.StopPumpAsync();
        var status = await _plcService.GetSystemStatusAsync();

        // Assert
        Assert.False(result);
        Assert.False(status.IsPumpRunning);
    }

    [Fact]
    public async Task ReadPressureAsync_ShouldReturnValidRange()
    {
        // Act
        var pressure = await _plcService.ReadPressureAsync();

        // Assert
        Assert.True(pressure >= 10.0 && pressure <= 15.0, $"Pressione fuori range: {pressure}");
    }

    [Fact]
    public async Task ReadTemperatureAsync_ShouldReturnValidRange()
    {
        // Act
        var temp = await _plcService.ReadTemperatureAsync();

        // Assert
        Assert.True(temp >= 20.0 && temp <= 30.0, $"Temperatura fuori range: {temp}");
    }

    [Fact]
    public async Task GetSystemStatusAsync_ShouldReturnCompleteSnapshot()
    {
        // Act
        var status = await _plcService.GetSystemStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.IsType<PlcSystemStatus>(status);
        Assert.True(status.Pressure >= 10.0 && status.Pressure <= 15.0);
        Assert.True(status.Temperature >= 20.0 && status.Temperature <= 30.0);
    }
}