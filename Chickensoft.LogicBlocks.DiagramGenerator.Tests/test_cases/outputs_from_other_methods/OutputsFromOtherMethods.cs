namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class OutputsFromOtherMethods :
LogicBlock
{
  public OutputsFromOtherMethods()
  {
    Set(new BaseState());
  }

  public void StartLogicBlock() => Start<BaseState>();
}
