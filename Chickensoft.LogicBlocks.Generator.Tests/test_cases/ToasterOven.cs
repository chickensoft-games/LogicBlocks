namespace Chickensoft.LogicBlocks.Generator.Tests;
[StateMachine]
public class ToasterOven :
  LogicBlock<ToasterOven.Input, ToasterOven.State, ToasterOven.Output> {
  public override State GetInitialState(IContext context) =>
    new State.Toasting(context, 0);

  public record Input {
    public record OpenDoor : Input;
    public record CloseDoor(int ToastColor) : Input;
    public record StartBaking(int Temperature) : Input;
    public record StartToasting(int ToastColor) : Input;
  }

  public abstract record State(IContext Context) : StateLogic(Context) {
    public record Heating : State, IGet<Input.OpenDoor> {
      public Heating(IContext context) : base(context) {
        OnEnter<Heating>(
          (previous) => Context.Output(new Output.TurnHeaterOn())
        );
        OnExit<Heating>(
          (next) => Context.Output(new Output.TurnHeaterOff())
        );
      }

      public State On(Input.OpenDoor input) => new DoorOpen(Context);
    }

    public record Toasting : Heating, IGet<Input.StartBaking> {
      public int ToastColor { get; }

      public Toasting(IContext context, int toastColor) : base(context) {
        ToastColor = toastColor;

        OnEnter<Toasting>(
          (previous) => Context.Output(new Output.SetTimer(ToastColor))
        );
        OnExit<Toasting>(
          (next) => Context.Output(new Output.ResetTimer())
        );
      }

      public State On(Input.StartBaking input) => new Baking(
        Context, input.Temperature
      );
    }

    public record Baking : Heating, IGet<Input.StartToasting> {
      public int Temperature { get; }

      public Baking(IContext context, int temperature) : base(context) {
        Temperature = temperature;

        OnEnter<Baking>(
          (previous) => Context.Output(new Output.SetTemperature(Temperature))
        );
        OnExit<Baking>(
          (next) => Context.Output(new Output.SetTemperature(0))
        );
      }

      public State On(Input.StartToasting input) => new Toasting(
        Context, input.ToastColor
      );
    }

    public record DoorOpen : State, IGet<Input.CloseDoor> {
      public DoorOpen(IContext context) : base(context) {
        OnEnter<DoorOpen>(
          (previous) => Context.Output(new Output.TurnLampOn())
        );
        OnExit<DoorOpen>(
          (next) => Context.Output(new Output.TurnLampOff())
        );
      }

      public State On(Input.CloseDoor input) => new Toasting(
        Context, input.ToastColor
      );
    }
  }

  public abstract record Output {
    public record TurnHeaterOn() : Output;
    public record TurnHeaterOff() : Output;
    public record SetTemperature(int Temp) : Output;
    public record TurnLampOn() : Output;
    public record TurnLampOff() : Output;
    public record SetTimer(int ToastColor) : Output;
    public record ResetTimer() : Output;
  }
}
