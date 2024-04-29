namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using Chickensoft.Introspection;

[Introspective("hierarchical_callback_logic")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class HierarchicalCallbackLogic :
  LogicBlock<HierarchicalCallbackLogic.State> {
  public Func<Transition> InitialState { get; init; } =
    () => default!;

  public override Transition GetInitialState() => InitialState();

  public static class Output {
    public readonly record struct Log(string Message);
  }

  [Introspective("hierarchical_callback_logic_state")]
  public partial record State : StateLogic<State> {
    public State() {
      this.OnEnter(() => Output(new Output.Log("state")));
      this.OnExit(() => Output(new Output.Log("state")));
    }

    [Introspective("hierarchical_callback_logic_substate")]
    public partial record Substate : State {
      public Substate() {
        this.OnEnter(() => Output(new Output.Log("substate")));
        this.OnExit(() => Output(new Output.Log("substate")));
      }
    }
  }
}
