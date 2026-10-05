namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class SharedBaseLogicBlock : SharedBaseLogic
{
  public SharedBaseLogicBlock()
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
