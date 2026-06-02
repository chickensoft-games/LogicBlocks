namespace Chickensoft.LogicBlocks.ScratchPad;

public partial class OverriddenHandlers
{
  public static class Input
  {
    public readonly record struct SomeInput;

    public readonly record struct SomeOtherInput;
  }

  public static class Output
  {
    public readonly record struct SomeOutput;

    public readonly record struct SomeOtherOutput;
  }

  [StateDiagram]
  public abstract record BaseState : LogicBlockState,
    IGet<Input.SomeInput>, IGet<Input.SomeOtherInput>
  {
    public virtual Type On(in Input.SomeInput input)
    {
      Output(new Output.SomeOutput());

      return ToSelf();
    }

    public Type On(in Input.SomeOtherInput input)
    {
      Output(new Output.SomeOtherOutput());

      return ToSelf();
    }

    public record Idle : BaseState
    {
      public override Type On(in Input.SomeInput input)
      {
        Output(new Output.SomeOtherOutput());

        return ToSelf();
      }
    }
  }
}
