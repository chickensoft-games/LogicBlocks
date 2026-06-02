namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

using LogicBlocks;

public partial class MyGenericType<T>
{
  public partial class GenericLogicBlock : LogicBlock
  {
    public GenericLogicBlock()
    {
      Set(new StateOne());
      Set(new StateTwo());
    }

    public void Start() => Start<StateOne>();
  }
}
