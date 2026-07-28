using System;
using System.Threading;
using System.Threading.Tasks;
using PlcBridge.Core.Interfaces;
using PlcBridge.Core.Models;
using PlcBridge.Infrastructure.Drivers;
using Xunit;

namespace PlcBridge.Tests;

public class PlcDriverTests
{
    [Fact]
    public async Task ConnectAsync_ShouldSetStateToConnected()
    {
        // Arrange
        IPlcDriver driver = new SimulatorPlcDriver();
        using var cts = new CancellationTokenSource();

        // Act
        await driver.ConnectAsync(cts.Token);

        // Assert
        Assert.Equal(ConnectionState.Connected, driver.State);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldSetStateToDisconnected()
    {
        // Arrange
        IPlcDriver driver = new SimulatorPlcDriver();
        using var cts = new CancellationTokenSource();
        await driver.ConnectAsync(cts.Token);

        // Act
        await driver.DisconnectAsync();

        // Assert
        Assert.Equal(ConnectionState.Disconnected, driver.State);
    }

    [Fact]
    public async Task ReadTagAsync_WhenNotConnected_ShouldThrowException()
    {
        // Arrange
        IPlcDriver driver = new SimulatorPlcDriver();
        using var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => driver.ReadTagAsync("PRESSURE", cts.Token));
    }

    [Fact]
    public async Task ReadTagAsync_Pressure_ShouldReturnDoubleValue()
    {
        // Arrange
        IPlcDriver driver = new SimulatorPlcDriver();
        using var cts = new CancellationTokenSource();
        await driver.ConnectAsync(cts.Token);

        // Act
        var result = await driver.ReadTagAsync("PRESSURE", cts.Token);

        // Assert
        Assert.IsType<double>(result);
        var pressureValue = (double)result;
        Assert.True(pressureValue >= 10.0 && pressureValue <= 15.0, "La pressione simulata dovrebbe essere tra 10.0 e 15.0");
    }

    [Fact]
    public async Task WriteAndRead_PumpStatus_ShouldUpdateValue()
    {
        // Arrange
        IPlcDriver driver = new SimulatorPlcDriver();
        using var cts = new CancellationTokenSource();
        await driver.ConnectAsync(cts.Token); // Connettendo, lo stato della pompa parte a 'false'

        // Act - Accendiamo la pompa
        await driver.WriteTagAsync("PUMP_STATUS", true, cts.Token);
        var status = await driver.ReadTagAsync("PUMP_STATUS", cts.Token);

        // Assert
        Assert.IsType<bool>(status);
        Assert.True((bool)status, "Il PUMP_STATUS dovrebbe essere true dopo la scrittura.");
    }
}