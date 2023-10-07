namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

[StateMachine]
public class HierarchicalCallbackLogic :
  LogicBlock<HierarchicalCallbackLogic.State> {
  public Func<IContext, State> InitialState { get; init; } =
    (context) => new State(context);

  public override State GetInitialState(IContext context) =>
    InitialState(context);

  public HierarchicalCallbackLogic(List<string> log) {
    Set(log);
  }

  public record State : StateLogic {
    public State(IContext context) : base(context) {
      OnEnter<State>((previous) => context.Get<List<string>>().Add("state"));
      OnExit<State>((previous) => context.Get<List<string>>().Add("state"));
    }

    public record Substate : State {
      public Substate(IContext context) : base(context) {
        OnEnter<Substate>(
          (previous) => context.Get<List<string>>().Add("substate")
        );
        OnExit<Substate>(
          (previous) => context.Get<List<string>>().Add("substate")
        );
      }
    }
  }
}
