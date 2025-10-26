namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

[LogicBlock(typeof(State)), Meta]
public partial class InputOnInitialState : LogicBlock<InputOnInitialState.State>
{
  public override Transition GetInitialState() => To<State>();

  public static class Input
  {
    public readonly record struct Start();
    public readonly record struct Initialize();
  }

  public partial record State : StateLogic<State>,
    IGet<Input.Start>,
    IGet<Input.Initialize>
  {
    public State()
    {
      this.OnEnter(() => Input(new Input.Initialize()));
    }

    public Transition On(in Input.Start input) => ToSelf();
    public Transition On(in Input.Initialize input) => ToSelf();
  }
}
