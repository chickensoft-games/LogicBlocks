namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateDiagram(typeof(State))]
public class ToasterOven : LogicBlock<ToasterOven.State> {
  public override State GetInitialState() => new State.Toasting(0);

  public static class Input {
    public readonly record struct OpenDoor;
    public readonly record struct CloseDoor(int ToastColor);
    public readonly record struct StartBaking(int Temperature);
    public readonly record struct StartToasting(int ToastColor);
  }

  public abstract record State : StateLogic<State> {
    public record Heating : State, IGet<Input.OpenDoor> {
      public Heating() {
        this.OnEnter(() => Context.Output(new Output.TurnHeaterOn()));
        this.OnExit(() => Context.Output(new Output.TurnHeaterOff()));
      }

      public State On(Input.OpenDoor input) => new DoorOpen();
    }

    public record Toasting : Heating, IGet<Input.StartBaking> {
      public int ToastColor { get; }

      public Toasting(int toastColor) {
        ToastColor = toastColor;

        this.OnEnter(() => Context.Output(new Output.SetTimer(ToastColor)));
        this.OnExit(() => Context.Output(new Output.ResetTimer()));
      }

      public State On(Input.StartBaking input) => new Baking(input.Temperature);
    }

    public record Baking : Heating, IGet<Input.StartToasting> {
      public int Temperature { get; }

      public Baking(int temperature) {
        Temperature = temperature;

        this.OnEnter(
          () => Context.Output(new Output.SetTemperature(Temperature))
        );
        this.OnExit(
          () => Context.Output(new Output.SetTemperature(0))
        );
      }

      public State On(Input.StartToasting input) => new Toasting(
        input.ToastColor
      );
    }

    public record DoorOpen : State, IGet<Input.CloseDoor> {
      public DoorOpen() {
        this.OnEnter(() => Context.Output(new Output.TurnLampOn()));
        this.OnExit(() => Context.Output(new Output.TurnLampOff()));
      }

      public State On(Input.CloseDoor input) => new Toasting(
        input.ToastColor
      );
    }
  }

  public static class Output {
    public readonly record struct TurnHeaterOn;
    public readonly record struct TurnHeaterOff;
    public readonly record struct SetTemperature(int Temp);
    public readonly record struct TurnLampOn;
    public readonly record struct TurnLampOff;
    public readonly record struct SetTimer(int ToastColor);
    public readonly record struct ResetTimer;
  }
}
