namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class NestedStartLogic : LogicBlock
{
  public NestedStartLogic()
  {
    Set(new BaseState.Running());
    Set(new BaseState.Idle());
  }

  public static class Input
  {
    public readonly record struct Toggle;
  }

  public static class Output
  {
    public readonly record struct Toggled;
  }
}
