namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

#pragma warning disable CS1998

[StateMachine]
public class HierarchicalCallbackLogicAsync :
  LogicBlockAsync<HierarchicalCallbackLogicAsync.State> {
  public Func<IContext, State> InitialState { get; init; } =
    (context) => new State(context);

  public override State GetInitialState(IContext context) =>
    InitialState(context);

  public HierarchicalCallbackLogicAsync(List<string> log) {
    Set(log);
  }

  public record State : StateLogic {
    public State(IContext context) : base(context) {
      OnEnter<State>(
        async (previous) => context.Get<List<string>>().Add("state")
      );
      OnExit<State>(
        async (previous) => context.Get<List<string>>().Add("state")
      );
    }

    public record Substate : State {
      public Substate(IContext context) : base(context) {
        OnEnter<Substate>(
          async (previous) => context.Get<List<string>>().Add("substate")
        );
        OnExit<Substate>(
          async (previous) => context.Get<List<string>>().Add("substate")
        );
      }
    }
  }
}

#pragma warning restore CS1998
