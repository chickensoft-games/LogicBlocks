namespace Chickensoft.LogicBlocks.Generator.Tests;

[LogicBlock(typeof(State), Diagram = true)]
public class ToasterOven : LogicBlock<ToasterOven.State> {
  public override Transition GetInitialState() => To<State.Toasting>()
    .With(toasting => ((State.Toasting)toasting).ToastColor = 0);

  public static class Input {
    public readonly record struct OpenDoor;
    public readonly record struct CloseDoor(int ToastColor);
    public readonly record struct StartBaking(int Temperature);
    public readonly record struct StartToasting(int ToastColor);
  }

  public abstract record State : StateLogic<State> {
    public record Heating : State, IGet<Input.OpenDoor> {
      public Heating() {
        this.OnEnter(() => Output(new Output.TurnHeaterOn()));
        this.OnExit(() => Output(new Output.TurnHeaterOff()));
      }

      public Transition On(Input.OpenDoor input) => To<DoorOpen>();
    }

    public record Toasting : Heating, IGet<Input.StartBaking> {
      public int ToastColor { get; set; }

      public Toasting(int toastColor) {
        ToastColor = toastColor;

        this.OnEnter(() => Output(new Output.SetTimer(ToastColor)));
        this.OnExit(() => Output(new Output.ResetTimer()));
      }

      public Transition On(Input.StartBaking input) => To<Baking>()
        .With(baking => ((Baking)baking).Temperature = input.Temperature);
    }

    public record Baking : Heating, IGet<Input.StartToasting> {
      public int Temperature { get; set; }

      public Baking(int temperature) {
        Temperature = temperature;

        this.OnEnter(
          () => Output(new Output.SetTemperature(Temperature))
        );
        this.OnExit(
          () => Output(new Output.SetTemperature(0))
        );
      }

      public Transition On(Input.StartToasting input) => To<Toasting>()
        .With(toasting => ((Toasting)toasting).ToastColor = input.ToastColor);
    }

    public record DoorOpen : State, IGet<Input.CloseDoor> {
      public DoorOpen() {
        this.OnEnter(() => Output(new Output.TurnLampOn()));
        this.OnExit(() => Output(new Output.TurnLampOff()));
      }

      public Transition On(Input.CloseDoor input) => To<Toasting>()
        .With(toasting => ((Toasting)toasting).ToastColor = input.ToastColor);
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
