namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitchInterface : LogicBlock<LightSwitchInterface.IState> {
  public override IState GetInitialState(IContext context) =>
    new State.TurnedOff();

  public interface IState : IStateLogic { }

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State : StateLogic, IState {
    // "On" state
    public record TurnedOn : State, IGet<Input.Toggle> {
      public IState On(Input.Toggle input) => new TurnedOff();
    }

    // "Off" state
    public record TurnedOff : State, IGet<Input.Toggle> {
      public IState On(Input.Toggle input) => new TurnedOn();
    }
  }
}
