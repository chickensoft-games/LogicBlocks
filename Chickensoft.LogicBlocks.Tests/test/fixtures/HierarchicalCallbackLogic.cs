namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using Chickensoft.LogicBlocks.Generator;

[StateDiagram(typeof(State))]
public class HierarchicalCallbackLogic :
  LogicBlock<HierarchicalCallbackLogic.State> {
  public Func<State> InitialState { get; init; } =
    () => default!;

  public override State GetInitialState() => InitialState();

  public static class Output {
    public readonly record struct Log(string Message);
  }

  public record State : StateLogic<State> {
    public State() {
      this.OnEnter(() => Output(new Output.Log("state")));
      this.OnExit(() => Output(new Output.Log("state")));
    }

    public record Substate : State {
      public Substate() : base() {
        this.OnEnter(() => Output(new Output.Log("substate")));
        this.OnExit(() => Output(new Output.Log("substate")));
      }
    }
  }
}
