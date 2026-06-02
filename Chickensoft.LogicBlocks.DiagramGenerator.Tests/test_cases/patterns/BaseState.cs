namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class Patterns
{
  public enum Mode { One, Two, Three }

  public static class Input
  {
    public readonly record struct SetMode(Mode Mode);
  }

  [StateDiagram]
  public abstract record BaseState : LogicBlockState, IGet<Input.SetMode>
  {
    public Type On(in Input.SetMode input) => input.Mode switch
    {
      Mode.One => To<One>(),
      Mode.Two => To<Two>(),
      Mode.Three => true switch
      {
        true => To<Three>(),
        false => throw new NotImplementedException()
      },
      _ => throw new NotImplementedException()
    };

    public record One : BaseState;

    public record Two : BaseState;

    public record Three : BaseState;
  }
}
