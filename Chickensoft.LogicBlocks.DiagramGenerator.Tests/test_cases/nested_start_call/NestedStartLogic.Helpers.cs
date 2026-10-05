namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class NestedStartLogic
{
  public class Helper
  {
    public void Start() { }

    public void Run() => Start();
  }
}
