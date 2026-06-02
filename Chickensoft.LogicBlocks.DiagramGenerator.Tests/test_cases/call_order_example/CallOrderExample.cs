namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

using System;

public partial class CallOrderExample : LogicBlock
{
  public CallOrderExample()
  {
    Set(new Standing());
    Set(new Walking());
    Set(new Running());
  }

  public void StartLogicBlock() => Start(typeof(Standing));
}
