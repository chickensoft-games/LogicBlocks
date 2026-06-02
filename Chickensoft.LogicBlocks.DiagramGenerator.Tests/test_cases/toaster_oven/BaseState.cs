namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class ToasterOven
{
  [StateDiagram]
  public abstract record BaseState : LogicBlockState
  {
    public record Heating : BaseState, IGet<Input.OpenDoor>
    {
      public Heating()
      {
        this.OnEnter(() => Output(new Output.TurnHeaterOn()));
        this.OnExit(() => Output(new Output.TurnHeaterOff()));
      }

      public Type On(in Input.OpenDoor input) => To<DoorOpen>();
    }

    public record Toasting : Heating, IGet<Input.StartBaking>
    {
      public Toasting()
      {
        var toastColor = Get<Data>().ToastColor;

        this.OnEnter(() => Output(new Output.SetTimer(toastColor)));
        this.OnExit(() => Output(new Output.ResetTimer()));
      }

      public Type On(in Input.StartBaking input)
      {
        Get<Data>().Temperature = input.Temperature;
        return To<Baking>();
      }
    }

    public record Baking : Heating, IGet<Input.StartToasting>
    {
      public Baking()
      {
        var temperature = Get<Data>().Temperature;

        this.OnEnter(() => Output(new Output.SetTemperature(temperature))
        );
        this.OnExit(() => Output(new Output.SetTemperature(0))
        );
      }

      public Type On(in Input.StartToasting input)
      {
        Get<Data>().ToastColor = input.ToastColor;
        return To<Toasting>();
      }
    }

    public record DoorOpen : BaseState, IGet<Input.CloseDoor>
    {
      public DoorOpen()
      {
        this.OnEnter(() => Output(new Output.TurnLampOn()));
        this.OnExit(() => Output(new Output.TurnLampOff()));
      }

      public Type On(in Input.CloseDoor input)
      {
        Get<Data>().ToastColor = input.ToastColor;
        return To<Toasting>();
      }
    }
  }

  public static class Output
  {
    public readonly record struct TurnHeaterOn;

    public readonly record struct TurnHeaterOff;

    public readonly record struct SetTemperature(int Temp);

    public readonly record struct TurnLampOn;

    public readonly record struct TurnLampOff;

    public readonly record struct SetTimer(int ToastColor);

    public readonly record struct ResetTimer;
  }
}
