namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

using System;

public partial class Patterns : LogicBlock
{
  public Patterns()
  {
    Set(new BaseState.One());
    Set(new BaseState.Two());
    Set(new BaseState.Three());
  }
  
  public void StartLogicBlock() => Start<BaseState.One>();
}
