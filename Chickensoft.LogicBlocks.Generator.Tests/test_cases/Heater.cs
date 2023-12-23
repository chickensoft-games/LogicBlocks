namespace Chickensoft.LogicBlocks.Generator.Tests;
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

[StateMachine]
public class Heater : LogicBlock<Heater.State> {
  public Heater(ITemperatureSensor tempSensor) {
    // Make sure states can access the temperature sensor.
    Set(tempSensor);
  }

  public override State GetInitialState() =>
    new State.Off() { TargetTemp = 72.0 };

  public static class Input {
    public readonly record struct TurnOn;
    public readonly record struct TurnOff;
    public readonly record struct TargetTempChanged(double Temp);
    public readonly record struct AirTempSensorChanged(double AirTemp);
  }

  public abstract record State : StateLogic, IGet<Input.TargetTempChanged> {
    public double TargetTemp { get; init; }

    public State() { }

    public State On(Input.TargetTempChanged input) => this with {
      TargetTemp = input.Temp
    };

    public abstract record Powered : State, IGet<Input.TurnOff> {
      public Powered() {
        // When we enter the state, subscribe to changes in temperature.
        OnAttach(
          () => {
            var tempSensor = Context.Get<ITemperatureSensor>();
            tempSensor.OnTemperatureChanged += OnTemperatureChanged;
          }
        );

        // When we exit this state, unsubscribe from changes in temperature.
        OnDetach(
          () => {
            var tempSensor = Context.Get<ITemperatureSensor>();
            tempSensor.OnTemperatureChanged -= OnTemperatureChanged;
          }
        );
      }

      public State On(Input.TurnOff input) =>
        new Off() { TargetTemp = TargetTemp };

      // Whenever our temperature sensor gives us a reading, we will just
      // provide an input to ourselves. This lets us have a chance to change
      // the logic block's state.
      private void OnTemperatureChanged(double airTemp) =>
        Context.Input(new Input.AirTempSensorChanged(airTemp));
    }

    public record Off : State, IGet<Input.TurnOn> {
      public Off() { }

      public State On(Input.TurnOn input) {
        // Get the temperature sensor from the blackboard.
        var tempSensor = Context.Get<ITemperatureSensor>();

        if (tempSensor.AirTemp >= TargetTemp) {
          // Room is already hot enough.
          return new Idle() { TargetTemp = TargetTemp };
        }

        // Room is too cold — start heating.
        return new Heating() { TargetTemp = TargetTemp };
      }
    }

    public record Idle : Powered, IGet<Input.AirTempSensorChanged> {
      public Idle() { }

      public State On(Input.AirTempSensorChanged input) {
        if (input.AirTemp < TargetTemp - 3.0d) {
          // Temperature has fallen too far below target temp — start heating.
          return new Heating() { TargetTemp = TargetTemp };
        }
        // Room is still hot enough — keep waiting.
        return this;
      }
    }

    public record Heating : Powered, IGet<Input.AirTempSensorChanged> {
      public Heating() { }

      public State On(Input.AirTempSensorChanged input) {
        if (input.AirTemp >= TargetTemp) {
          // We're done heating!
          Context.Output(new Output.FinishedHeating());
          return new Idle() { TargetTemp = TargetTemp };
        }
        // Room isn't hot enough — keep heating.
        return this;
      }
    }
  }

  public static class Output {
    public readonly record struct FinishedHeating;
  }
}
