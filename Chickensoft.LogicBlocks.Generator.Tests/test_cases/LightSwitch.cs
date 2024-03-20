namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateDiagram(typeof(State))]
public class LightSwitch : LogicBlock<LightSwitch.State> {
  public override State GetInitialState() => new State.Off();

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State : StateLogic<State> {
    public record On : State, IGet<Input.Toggle> {
      public On() {
        // Announce that we are now on.
        this.OnEnter(() => Output(new Output.StatusChanged(IsOn: true)));
      }

      State IGet<Input.Toggle>.On(in Input.Toggle input) => new Off();
    }

    public record Off : State, IGet<Input.Toggle> {
      public Off() {
        // Announce that we are now off.
        this.OnEnter(() => Output(new Output.StatusChanged(IsOn: false)));
      }

      State IGet<Input.Toggle>.On(in Input.Toggle input) => new On();
    }
  }

  public static class Output {
    public readonly record struct StatusChanged(bool IsOn);
  }
}
