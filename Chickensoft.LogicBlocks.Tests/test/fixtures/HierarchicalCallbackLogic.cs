namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

[StateDiagram(typeof(State))]
public class HierarchicalCallbackLogic :
  LogicBlock<HierarchicalCallbackLogic.State> {
  public Func<State> InitialState { get; init; } =
    () => new State();

  public override State GetInitialState() => InitialState();

  public HierarchicalCallbackLogic(List<string> log) {
    Set(log);
  }

  public record State : StateLogic<State> {
    public State() {
      this.OnEnter(() => Context.Get<List<string>>().Add("state"));
      this.OnExit(() => Context.Get<List<string>>().Add("state"));
    }

    public record Substate : State {
      public Substate() {
        this.OnEnter(() => Context.Get<List<string>>().Add("substate"));
        this.OnExit(() => Context.Get<List<string>>().Add("substate"));
      }
    }
  }
}
