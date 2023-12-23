namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

#pragma warning disable CS1998

[StateMachine]
public class HierarchicalCallbackLogicAsync :
  LogicBlockAsync<HierarchicalCallbackLogicAsync.State> {
  public Func<State> InitialState { get; init; } =
    () => new State();

  public override State GetInitialState(IContext context) =>
    InitialState();

  public HierarchicalCallbackLogicAsync(List<string> log) {
    Set(log);
  }

  public record State : StateLogic {
    public State() {
      OnEnter<State>(
        async (previous) => Context.Get<List<string>>().Add("state")
      );
      OnExit<State>(
        async (next) => Context.Get<List<string>>().Add("state")
      );
    }

    public record Substate : State {
      public Substate() {
        OnEnter<Substate>(
          async (previous) => Context.Get<List<string>>().Add("substate")
        );
        OnExit<Substate>(
          async (next) => Context.Get<List<string>>().Add("substate")
        );
      }
    }
  }
}

#pragma warning restore CS1998
