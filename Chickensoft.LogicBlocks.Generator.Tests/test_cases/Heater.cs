namespace Chickensoft.LogicBlocks.Generator.Tests;

using Shouldly;

public interface ITemperatureSensor {
  event Action<double>? OnTemperatureChanged;
}

public record TemperatureSensor : ITemperatureSensor {
  public event Action<double>? OnTemperatureChanged;

  public void UpdateReading(double airTemp) =>
    OnTemperatureChanged?.Invoke(airTemp);
}

[StateMachine]
public class Heater :
  LogicBlock<Heater.Input, Heater.State, Heater.Output> {
  public Heater(ITemperatureSensor tempSensor) {
    // Make sure states can access the temperature sensor.
    Set(tempSensor);
  }

  public override State GetInitialState(Context context) =>
    new State.Off(context, 72.0);

  public abstract record Input {
    public record TurnOn : Input;
    public record TurnOff : Input;
    public record TargetTempChanged(double Temp) : Input;
    public record AirTempSensorChanged(double AirTemp) : Input;
  }

  public abstract record State(Context Context, double TargetTemp)
    : StateLogic(Context) {
    public record Off(
      Context Context, double TargetTemp
    ) : State(Context, TargetTemp), IGet<Input.TurnOn> {
      State IGet<Input.TurnOn>.On(Input.TurnOn input) =>
        new Heating(Context, TargetTemp);
    }

    public record Idle(Context Context, double TargetTemp) :
      State(Context, TargetTemp);

    public record Heating : State,
      IGet<Input.TurnOff>,
      IGet<Input.AirTempSensorChanged>,
      IGet<Input.TargetTempChanged> {
      public Heating(Context context, double targetTemp) : base(
        context, targetTemp
      ) {
        var tempSensor = context.Get<ITemperatureSensor>();

        OnEnter<Heating>(
          (previous) => tempSensor.OnTemperatureChanged += OnTemperatureChanged
        );

        OnExit<Heating>(
          (next) => tempSensor.OnTemperatureChanged -= OnTemperatureChanged
        );
      }

      public State On(Input.TurnOff input) => new Off(Context, TargetTemp);

      public State On(Input.AirTempSensorChanged input) =>
        input.AirTemp >= TargetTemp
          ? new Idle(Context, TargetTemp)
          : this;

      public State On(Input.TargetTempChanged input) => this with {
        TargetTemp = input.Temp
      };

      private void OnTemperatureChanged(double airTemp) {
        Context.Input(new Input.AirTempSensorChanged(airTemp));
        Context.Output(new Output.AirTempChanged(airTemp));
      }
    }
  }

  public abstract record Output {
    public record AirTempChanged(double AirTemp) : Output;
  }
}

public static class Program2 {
  public static void Main2(string[] args) {
    var tempSensor = new TemperatureSensor();
    var heater = new Heater(tempSensor);

    var binding = heater.Bind();

    binding.Handle<Heater.Output.AirTempChanged>(
      (output) => Console.WriteLine($"Air temp changed to {output.AirTemp}")
    );

    binding.When<Heater.State.Off>().Call(
      (state) => Console.WriteLine("Heater is off")
    );

    binding.When<Heater.State.Idle>().Call(
      (state) => Console.WriteLine("Heater is idle")
    );

    binding.When<Heater.State>()
      .Use(
        data: (state) => state.TargetTemp,
        to: (temp) => Console.WriteLine($"Heater target temp is {temp}")
      );

    heater.Input(new Heater.Input.TurnOn());

    tempSensor.UpdateReading(58.0);

    heater.Value.TargetTemp.ShouldBe(72);
  }
}
