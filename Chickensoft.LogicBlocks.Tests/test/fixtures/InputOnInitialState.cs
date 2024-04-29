namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

[Introspective("input_on_initial_state")]
[LogicBlock(typeof(State))]
public partial class InputOnInitialState : LogicBlock<InputOnInitialState.State> {
  public override Transition GetInitialState() => To<State>();

  public static class Input {
    public readonly record struct Start();
    public readonly record struct Initialize();
  }

  [Introspective("input_on_initial_state_state")]
  public partial record State : StateLogic<State>,
    IGet<Input.Start>,
    IGet<Input.Initialize> {
    public State() {
      this.OnEnter(() => Input(new Input.Initialize()));
    }

    public Transition On(Input.Start input) => ToSelf();
    public Transition On(Input.Initialize input) => ToSelf();
  }
}
