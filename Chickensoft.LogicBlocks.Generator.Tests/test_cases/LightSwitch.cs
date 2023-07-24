namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitch : LogicBlock<LightSwitch.Input, LightSwitch.State, LightSwitch.Output> {
  public override State GetInitialState(Context context) =>
    new State.Off(context);

  public abstract record Input {
    public record Toggle : Input;
  }

  public abstract record State(Context Context) : StateLogic(Context) {
    public record On(Context Context) : State(Context), IGet<Input.Toggle> {
      State IGet<Input.Toggle>.On(Input.Toggle input) => new Off(Context);
    }

    public record Off(Context Context) : State(Context), IGet<Input.Toggle> {
      State IGet<Input.Toggle>.On(Input.Toggle input) => new On(Context);
    }
  }

  public abstract record Output { }
}
