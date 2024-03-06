namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateDiagram(typeof(State))]
public class SingleState : LogicBlock<SingleState.State> {
  public override State GetInitialState() => new();

  public static class Input {
    public readonly record struct MyInput;
  }
  public record State : StateLogic<State>, IGet<Input.MyInput> {
    public State() {
      this.OnEnter(() => Context.Output(new Output.MyOutput()));
      this.OnExit(() => Context.Output(new Output.MyOutput()));
    }

    public State On(Input.MyInput input) {
      Context.Output(new Output.MyOutput());
      return this;
    }
  }
  public static class Output {
    public readonly record struct MyOutput;
  }
}
