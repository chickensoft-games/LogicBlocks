namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

using System;
using Sync.Primitives;

/// <summary>
/// Temperature sensor that presumably communicates with actual hardware
/// (not shown here).
/// </summary>
public interface ITemperatureSensor
{
  /// <summary>Last recorded air temperature.</summary>
  double AirTemp { get; }
  /// <summary>Invoked whenever a change in temperature is noticed.</summary>
  AutoValue<double> Temperature { get; }
}

public record TemperatureSensor : ITemperatureSensor
{
  public double AirTemp { get; private set; } = 72.0d;
  public AutoValue<double> Temperature { get; } = new(0);

  public void UpdateReading(double airTemp)
  {
    AirTemp = airTemp;
    Temperature.Value = airTemp;
  }
}

public partial class Heater : LogicBlock
{
  public Heater()
  {
    Set(new HeaterState.Off());
    Set(new HeaterState.Idle());
    Set(new HeaterState.Heating());
    Set(new Data { TargetTemp = 72.0 });
  }

  public void StartLogicBlock() => Start<HeaterState.Off>();
}
