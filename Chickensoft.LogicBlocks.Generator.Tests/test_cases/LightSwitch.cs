namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateDiagram(typeof(State))]
public class LightSwitch : LogicBlock<LightSwitch.State> {
  public override State GetInitialState() => new State.Powered();

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State : StateLogic<State> {
    public record PoweredOn : State, IGet<Input.Toggle> {
      public PoweredOn() {
        // Announce that we are now on.
        this.OnEnter(() => Output(new Output.StatusChanged(IsOn: true)));
      }

      public State On(in Input.Toggle input) => new Powered();
    }

    public record Powered : State, IGet<Input.Toggle> {
      public Powered() {
        // Announce that we are now off.
        this.OnEnter(() => Output(new Output.StatusChanged(IsOn: false)));
      }

      public State On(in Input.Toggle input) => new PoweredOn();
    }
  }

  public static class Output {
    public readonly record struct StatusChanged(bool IsOn);
  }
}
