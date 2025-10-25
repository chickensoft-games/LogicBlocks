namespace Chickensoft.LogicBlocks.ScratchPad;

using Chickensoft.Introspection;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class OverriddenHandlers : LogicBlock<OverriddenHandlers.State>
{
  public override Transition GetInitialState() => To<State.Idle>();

  public static class Input
  {
    public readonly record struct SomeInput();
    public readonly record struct SomeOtherInput();

  }

  public static class Output
  {
    public readonly record struct SomeOutput();
    public readonly record struct SomeOtherOutput();
  }

  public abstract record State : StateLogic<State>,
  IGet<Input.SomeInput>, IGet<Input.SomeOtherInput>
  {

    public virtual Transition On(in Input.SomeInput input)
    {
      Output(new Output.SomeOutput());

      return ToSelf();
    }

    public Transition On(in Input.SomeOtherInput input)
    {
      Output(new Output.SomeOtherOutput());

      return ToSelf();
    }

    public record Idle : State
    {
      public override Transition On(in Input.SomeInput input)
      {
        Output(new Output.SomeOtherOutput());

        return ToSelf();
      }
    }
  }
}
