namespace Chickensoft.LogicBlocks.Tests.Examples;

[LogicBlock(typeof(State), Diagram = true)]
public class LightSwitch : LogicBlock<LightSwitch.State> {
  public override Transition GetInitialState() => To<State.PoweredOff>();

  public LightSwitch() {
    Set(new State.PoweredOn());
    Set(new State.PoweredOff());
  }

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State : StateLogic<State> {
    public record PoweredOn : State, IGet<Input.Toggle> {
      public PoweredOn() {
        // Announce that we are now on.
        this.OnEnter(() => Output(new Output.StatusChanged(IsOn: true)));
      }

      public Transition On(in Input.Toggle input) => To<PoweredOff>();
    }

    public record PoweredOff : State, IGet<Input.Toggle> {
      public PoweredOff() {
        // Announce that we are now off.
        this.OnEnter(() => Output(new Output.StatusChanged(IsOn: false)));
      }

      public Transition On(in Input.Toggle input) => To<PoweredOn>();
    }
  }

  public static class Output {
    public readonly record struct StatusChanged(bool IsOn);
  }
}
