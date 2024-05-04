namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

[LogicBlock(typeof(State), Diagram = true)]
public class LightSwitch : LogicBlock<LightSwitch.State> {
  public override Transition GetInitialState() => To<State.Powered>();

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State : StateLogic<State> {
    public record PoweredOn : State, IGet<Input.Toggle> {
      public PoweredOn() {
        // Announce that we are now on.
        this.OnEnter(() => Output(new Output.StatusChanged(IsOn: true)));
      }

      public Transition On(in Input.Toggle input) => To<Powered>();
    }

    public record Powered : State, IGet<Input.Toggle> {
      public Powered() {
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
