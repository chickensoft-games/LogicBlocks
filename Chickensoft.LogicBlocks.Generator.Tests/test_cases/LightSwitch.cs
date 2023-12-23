namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitch : LogicBlock<LightSwitch.State> {
  public override State GetInitialState() => new State.SwitchedOff();

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State : StateLogic {
    // "On" state
    public record SwitchedOn : State, IGet<Input.Toggle> {
      public State On(Input.Toggle input) => new SwitchedOff();
    }

    // "Off" state
    public record SwitchedOff : State, IGet<Input.Toggle> {
      public State On(Input.Toggle input) => new SwitchedOn();
    }
  }
}
