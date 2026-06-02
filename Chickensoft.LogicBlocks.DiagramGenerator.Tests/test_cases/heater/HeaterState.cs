namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class Heater
{
  public record Data
  {
    public double TargetTemp { get; set; }
  }

  public override IEnumerable<IDisposable> OnStartSubscriptions()
  {
    yield return Get<ITemperatureSensor>().Temperature.Bind()
      .OnValue(OnTemperatureChanged);
  }

  private void OnTemperatureChanged(double airTemp) =>
    Input(new Input.AirTempSensorChanged(airTemp));

  public static class Input
  {
    public readonly record struct TurnOn;
    public readonly record struct TurnOff;
    public readonly record struct TargetTempChanged(double Temp);
    public readonly record struct AirTempSensorChanged(double AirTemp);
  }

  public static class Output
  {
    public readonly record struct FinishedHeating;
    public readonly record struct Chime;
  }

  [StateDiagram]
  public abstract record HeaterState :
    LogicBlockState, IGet<Input.TargetTempChanged>
  {
    public Type On(in Input.TargetTempChanged input)
    {
      Get<Data>().TargetTemp = input.Temp;
      return ToSelf();
    }

    public abstract record Powered : HeaterState, IGet<Input.TurnOff>
    {
      public Powered()
      {
        // Whenever a Powered state is entered, play a chime to
        // alert the user that the heater is on. Subsequent states that
        // inherit from Powered will not play a chime until a different
        // state has been entered before returning to a Powered state.
        this.OnEnter(() => Output(new Output.Chime()));

        // Unlike OnEnter, OnAttach will run for every state instance that
        // inherits from this record. Use these to setup your state.
        //
        // Attach and detach are great for setting up long-running operations.
      }

      public Type On(in Input.TurnOff input) =>
        To<Off>();

      // Whenever our temperature sensor gives us a reading, we will just
      // provide an input to ourselves. This lets us have a chance to change
      // the logic block's state.
    }

    public record Off : HeaterState, IGet<Input.TurnOn>
    {
      public Type On(in Input.TurnOn input)
      {
        // Get the temperature sensor from the blackboard.
        var tempSensor = Get<ITemperatureSensor>();
        var targetTemp = Get<Data>().TargetTemp;

        if (tempSensor.AirTemp >= targetTemp)
        {
          // Room is already hot enough.
          return To<Idle>();
        }

        // Room is too cold — start heating.
        return To<Heating>();
      }
    }

    public record Idle : Powered, IGet<Input.AirTempSensorChanged>
    {
      public Type On(in Input.AirTempSensorChanged input)
      {
        var targetTemp = Get<Data>().TargetTemp;
        if (input.AirTemp < targetTemp - 3.0d)
        {
          // Temperature has fallen too far below target temp — start heating.
          return To<Heating>();
        }
        // Room is still hot enough — keep waiting.
        return ToSelf();
      }
    }

    public record Heating : Powered, IGet<Input.AirTempSensorChanged>
    {
      public Type On(in Input.AirTempSensorChanged input)
      {
        var targetTemp = Get<Data>().TargetTemp;
        if (input.AirTemp >= targetTemp)
        {
          // We're done heating!
          Output(new Output.FinishedHeating());
          return To<Idle>();
        }
        // Room isn't hot enough — keep heating.
        return ToSelf();
      }
    }
  }
}
