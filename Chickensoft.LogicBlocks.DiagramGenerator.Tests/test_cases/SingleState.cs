namespace Chickensoft.LogicBlocks.Generator.Tests;

[LogicBlock(typeof(State), Diagram = true)]
public class SingleState : LogicBlock<SingleState.State> {
  public override Transition GetInitialState() => To<State>();

  public static class Input {
    public readonly record struct MyInput;
  }
  public record State : StateLogic<State>, IGet<Input.MyInput> {
    public State() {
      this.OnEnter(() => Output(new Output.MyOutput()));
      this.OnExit(() => Output(new Output.MyOutput()));
    }

    public Transition On(Input.MyInput input) {
      Output(new Output.MyOutput());
      return ToSelf();
    }
  }
  public static class Output {
    public readonly record struct MyOutput;
  }
}
