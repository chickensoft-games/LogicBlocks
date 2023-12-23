namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitch : LogicBlock<LightSwitch.State> {
  public override State GetInitialState() => new State.TurnedOff();

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State : StateLogic {
    // "On" state
    public record TurnedOn : State, IGet<Input.Toggle> {
      public TurnedOn() { }

      public State On(Input.Toggle input) => new TurnedOff();
    }

    // "Off" state
    public record TurnedOff : State, IGet<Input.Toggle> {
      public TurnedOff() { }

      public State On(Input.Toggle input) => new TurnedOn();
    }
  }
}
