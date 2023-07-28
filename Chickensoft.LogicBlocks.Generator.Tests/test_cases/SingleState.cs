namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class SingleState :
  LogicBlock<SingleState.Input, SingleState.State, SingleState.Output> {
  public override State GetInitialState(Context context) => new(Context);

  public abstract record Input {
    public record MyInput : Input { }
  }
  public record State : StateLogic, IGet<Input.MyInput> {
    public State(Context context) : base(context) {
      OnEnter<State>((previous) => Context.Output(new Output.MyOutput()));
      OnExit<State>((next) => Context.Output(new Output.MyOutput()));
    }

    public State On(Input.MyInput input) {
      Context.Output(new Output.MyOutput());
      return this;
    }
  }
  public abstract record Output {
    public record MyOutput : Output { }
  }
}
