namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

[StateMachine]
public class HierarchicalCallbackLogic :
  LogicBlock<HierarchicalCallbackLogic.State> {
  public Func<State> InitialState { get; init; } =
    () => new State();

  public override State GetInitialState() => InitialState();

  public HierarchicalCallbackLogic(List<string> log) {
    Set(log);
  }

  public record State : StateLogic {
    public State() {
      OnEnter<State>((previous) => Context.Get<List<string>>().Add("state"));
      OnExit<State>((next) => Context.Get<List<string>>().Add("state"));
    }

    public record Substate : State {
      public Substate() {
        OnEnter<Substate>(
          (previous) => Context.Get<List<string>>().Add("substate")
        );
        OnExit<Substate>(
          (next) => Context.Get<List<string>>().Add("substate")
        );
      }
    }
  }
}
