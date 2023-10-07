namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitch : LogicBlock<LightSwitch.State> {
  public override State GetInitialState(IContext context) =>
    new State.TurnedOff(context);

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State(IContext Context) : StateLogic(Context) {
    // "On" state
    public record TurnedOn : State, IGet<Input.Toggle> {
      public TurnedOn(IContext context) : base(context) { }

      public State On(Input.Toggle input) => new TurnedOff(Context);
    }

    // "Off" state
    public record TurnedOff : State, IGet<Input.Toggle> {
      public TurnedOff(IContext context) : base(context) { }

      public State On(Input.Toggle input) => new TurnedOn(Context);
    }
  }
}
