namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class SingleState : LogicBlock
{
  public SingleState()
  {
    Set(new BaseState());
  }

  public void StartLogicBlock() => Start<BaseState>();
}
