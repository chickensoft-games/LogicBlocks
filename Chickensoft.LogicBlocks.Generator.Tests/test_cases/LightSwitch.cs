namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitch : LogicBlock<LightSwitch.Input, LightSwitch.State, LightSwitch.Output> {
  public override State GetInitialState(IContext context) =>
    new State.Off(context);

  public abstract record Input {
    public record Toggle : Input;
  }

  public abstract record State(IContext Context) : StateLogic(Context) {
    public record On(IContext Context) : State(Context), IGet<Input.Toggle> {
      State IGet<Input.Toggle>.On(Input.Toggle input) => new Off(Context);
    }

    public record Off(IContext Context) : State(Context), IGet<Input.Toggle> {
      State IGet<Input.Toggle>.On(Input.Toggle input) => new On(Context);
    }
  }

  public abstract record Output { }
}
