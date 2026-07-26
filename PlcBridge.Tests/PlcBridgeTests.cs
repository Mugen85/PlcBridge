// using Xunit;

// public class PlcBridgeTests
// {
//     private readonly PlcController _plc;

//     public PlcBridgeTests()
//     {
//         // Arrange: Inizializziamo il controller prima di ogni test
//         _plc = new PlcController();
//     }

//     [Fact]
//     public void StartPump_ShouldSetPumpToRunning()
//     {
//         // Act
//         var result = _plc.StartPump();

//         // Assert
//         Assert.Contains("RUNNING", result);
//         Assert.Contains("SUCCESS", result);
//     }

//     [Fact]
//     public void StopPump_ShouldSetPumpToStopped()
//     {
//         // Act
//         _plc.StartPump(); // Prima la accendiamo
//         var result = _plc.StopPump();

//         // Assert
//         Assert.Contains("STOPPED", result);
//         Assert.Contains("SUCCESS", result);
//     }

//     [Fact]
//     public void ReadPressure_ShouldReturnCorrectFormat()
//     {
//         // Act
//         var result = _plc.ReadPressure();

//         // Assert
//         Assert.StartsWith("PRESSURE:", result);
//         Assert.Contains("BAR", result);
//     }

//     [Fact]
//     public void GetSystemStatus_ShouldReflectCurrentState()
//     {
//         // Act
//         _plc.StartPump();
//         var status = _plc.GetSystemStatus();

//         // Assert
//         Assert.Contains("RUNNING", status);
//     }
// }