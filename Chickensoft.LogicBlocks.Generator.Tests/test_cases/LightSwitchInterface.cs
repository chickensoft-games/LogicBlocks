namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateDiagram(typeof(State))]
public class LightSwitchInterface : LogicBlock<LightSwitchInterface.IState> {
  public override IState GetInitialState() => new State.TurnedOff();

  public interface IState : IStateLogic<IState> { }

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State : StateLogic<IState>, IState {
    // "On" state
    public record TurnedOn : State, IGet<Input.Toggle> {
      public IState On(in Input.Toggle input) => new TurnedOff();
    }

    // "Off" state
    public record TurnedOff : State, IGet<Input.Toggle> {
      public IState On(in Input.Toggle input) => new TurnedOn();
    }
  }
}
