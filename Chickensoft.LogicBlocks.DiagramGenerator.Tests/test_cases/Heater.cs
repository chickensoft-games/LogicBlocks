namespace Chickensoft.LogicBlocks.Generator.Tests;

using System;
using Chickensoft.Introspection;

/// <summary>
/// Temperature sensor that presumably communicates with actual hardware
/// (not shown here).
/// </summary>
public interface ITemperatureSensor {
  /// <summary>Last recorded air temperature.</summary>
  double AirTemp { get; }
  /// <summary>Invoked whenever a change in temperature is noticed.</summary>
  event Action<double>? OnTemperatureChanged;
}

public record TemperatureSensor : ITemperatureSensor {
  public double AirTemp { get; set; } = 72.0d;
  public event Action<double>? OnTemperatureChanged;

  public void UpdateReading(double airTemp) {
    AirTemp = airTemp;
    OnTemperatureChanged?.Invoke(airTemp);
  }
}

[Introspective("heater")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class Heater : LogicBlock<Heater.State> {
  public override Transition GetInitialState() =>
    To<State.Off>().With(off => off.TargetTemp = 72.0);

  public static class Input {
    public readonly record struct TurnOn;
    public readonly record struct TurnOff;
    public readonly record struct TargetTempChanged(double Temp);
    public readonly record struct AirTempSensorChanged(double AirTemp);
  }
  [Introspective("heater_state")]
  public abstract partial record State :
    StateLogic<State>, IGet<Input.TargetTempChanged> {
    public double TargetTemp { get; set; }

    public Transition On(Input.TargetTempChanged input) =>
      ToSelf().With(state => state.TargetTemp = input.Temp);

    [Introspective("heater_state_powered")]
    public abstract partial record Powered : State, IGet<Input.TurnOff> {
      public Powered() {
        // Whenever a Powered state is entered, play a chime to
        // alert the user that the heater is on. Subsequent states that
        // inherit from Powered will not play a chime until a different
        // state has been entered before returning to a Powered state.
        this.OnEnter(() => Output(new Output.Chime()));

        // Unlike OnEnter, OnAttach will run for every state instance that
        // inherits from this record. Use these to setup your state.
        //
        // Attach and detach are great for setting up long-running operations.
        OnAttach(
          () => Get<ITemperatureSensor>().OnTemperatureChanged += OnTemperatureChanged
        );

        OnDetach(
          () => Get<ITemperatureSensor>().OnTemperatureChanged -= OnTemperatureChanged
        );
      }

      public Transition On(Input.TurnOff input) =>
        To<Off>().With(off => off.TargetTemp = TargetTemp);

      // Whenever our temperature sensor gives us a reading, we will just
      // provide an input to ourselves. This lets us have a chance to change
      // the logic block's state.
      private void OnTemperatureChanged(double airTemp) =>
        Input(new Input.AirTempSensorChanged(airTemp));
    }

    [Introspective("heater_state_off")]
    public partial record Off : State, IGet<Input.TurnOn> {
      public Transition On(Input.TurnOn input) {
        // Get the temperature sensor from the blackboard.
        var tempSensor = Get<ITemperatureSensor>();

        if (tempSensor.AirTemp >= TargetTemp) {
          // Room is already hot enough.
          return To<Idle>().With(idle => idle.TargetTemp = TargetTemp);
        }

        // Room is too cold — start heating.
        return To<Heating>().With(heating => heating.TargetTemp = TargetTemp);
      }
    }

    [Introspective("heater_state_powered_idle")]
    public partial record Idle : Powered, IGet<Input.AirTempSensorChanged> {
      public Transition On(Input.AirTempSensorChanged input) {
        if (input.AirTemp < TargetTemp - 3.0d) {
          // Temperature has fallen too far below target temp — start heating.
          return To<Heating>()
            .With(heating => heating.TargetTemp = TargetTemp);
        }
        // Room is still hot enough — keep waiting.
        return ToSelf();
      }
    }

    [Introspective("heater_state_powered_heating")]
    public partial record Heating : Powered, IGet<Input.AirTempSensorChanged> {
      public Transition On(Input.AirTempSensorChanged input) {
        if (input.AirTemp >= TargetTemp) {
          // We're done heating!
          Output(new Output.FinishedHeating());
          return To<Idle>().With(idle => idle.TargetTemp = TargetTemp);
        }
        // Room isn't hot enough — keep heating.
        return ToSelf();
      }
    }
  }

  public static class Output {
    public readonly record struct FinishedHeating;
    public readonly record struct Chime;
  }
}
