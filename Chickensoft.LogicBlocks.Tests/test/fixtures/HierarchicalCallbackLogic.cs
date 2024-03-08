namespace Chickensoft.LogicBlocks.Tests.Fixtures;

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
      this.OnEnter(
        () => Context.Output(new Output.Log("state"))
      );
      this.OnExit(
        () => Context.Output(new Output.Log("state"))
      );
    }

    public record Substate : State {
      public Substate() : base() {
        this.OnEnter(
          () => Context.Output(new Output.Log("substate"))
        );
        this.OnExit(
          () => Context.Output(new Output.Log("substate"))
        );
      }
    }
  }
}
