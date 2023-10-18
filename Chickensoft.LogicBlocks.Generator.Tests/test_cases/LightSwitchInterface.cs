namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitchInterface : LogicBlock<LightSwitchInterface.IState> {
  public override IState GetInitialState(IContext context) =>
    new State.TurnedOff(context);

  public interface IState : IStateLogic { }

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State(IContext Context) : StateLogic(Context), IState {
    // "On" state
    public record TurnedOn : State, IGet<Input.Toggle> {
      public TurnedOn(IContext context) : base(context) { }

      public IState On(Input.Toggle input) => new TurnedOff(Context);
    }

    // "Off" state
    public record TurnedOff : State, IGet<Input.Toggle> {
      public TurnedOff(IContext context) : base(context) { }

      public IState On(Input.Toggle input) => new TurnedOn(Context);
    }
  }
}
