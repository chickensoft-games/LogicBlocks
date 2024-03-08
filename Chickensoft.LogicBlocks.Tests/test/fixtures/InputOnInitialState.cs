namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class InputOnInitialState : LogicBlock<InputOnInitialState.State> {
  public override State GetInitialState() => new();

  public static class Input {
    public readonly record struct Start();
    public readonly record struct Initialize();
  }

  public record State : StateLogic<State>,
    IGet<Input.Start>,
    IGet<Input.Initialize> {
    public State() {
      this.OnEnter(() => Context.Input(new Input.Initialize()));
    }

    public State On(in Input.Start input) => this;
    public State On(in Input.Initialize input) => this;
  }
}
