namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class SingleState : LogicBlock<SingleState.State> {
  public override State GetInitialState(IContext context) => new();

  public static class Input {
    public readonly record struct MyInput;
  }
  public record State : StateLogic, IGet<Input.MyInput> {
    public State() {
      OnEnter<State>((previous) => Context.Output(new Output.MyOutput()));
      OnExit<State>((next) => Context.Output(new Output.MyOutput()));
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
