namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using Chickensoft.Introspection;

[LogicBlock(typeof(State), Diagram = true), Meta]
public partial class HierarchicalCallbackLogic :
  LogicBlock<HierarchicalCallbackLogic.State> {
  public Func<Transition> InitialState { get; init; } =
    () => default!;

  public override Transition GetInitialState() => InitialState();

  public static class Output {
    public readonly record struct Log(string Message);
  }

  public partial record State : StateLogic<State> {
    public State() {
      this.OnEnter(() => Output(new Output.Log("state")));
      this.OnExit(() => Output(new Output.Log("state")));
    }

    public partial record Substate : State {
      public Substate() {
        this.OnEnter(() => Output(new Output.Log("substate")));
        this.OnExit(() => Output(new Output.Log("substate")));
      }
    }
  }
}
