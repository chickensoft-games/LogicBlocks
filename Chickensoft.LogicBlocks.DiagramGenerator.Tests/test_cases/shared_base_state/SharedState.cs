namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class SharedBaseStateLogic
{
  [StateDiagram]
  public abstract partial record SharedState : SharedBaseState;
}
