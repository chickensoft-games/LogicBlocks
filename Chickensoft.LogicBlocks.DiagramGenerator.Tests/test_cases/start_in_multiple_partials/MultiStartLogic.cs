namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class MultiStartLogic : LogicBlock
{
  public MultiStartLogic()
  {
    Set(new BaseState.Running());
    Set(new BaseState.Idle());
  }

  public void StartLogicBlock() => Start<BaseState.Idle>();

  public static class Input
  {
    public readonly record struct Toggle;
  }

  public static class Output
  {
    public readonly record struct Toggled;
  }
}
