namespace Chickensoft.LogicBlocks.Generator.Tests;

[LogicBlock(typeof(State), Diagram = true)]
public class SingleState : LogicBlock<SingleState.State> {
  public override State GetInitialState() => new();

  public static class Input {
    public readonly record struct MyInput;
  }
  public record State : StateLogic<State>, IGet<Input.MyInput> {
    public State() {
      this.OnEnter(() => Output(new Output.MyOutput()));
      this.OnExit(() => Output(new Output.MyOutput()));
    }

    public State On(in Input.MyInput input) {
      Output(new Output.MyOutput());
      return this;
    }
  }
  public static class Output {
    public readonly record struct MyOutput;
  }
}
