namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class OutputsFromOtherMethods
{
  [StateDiagram]
  public record BaseState : LogicBlockState
  {
    public void Method1() => Output(new Output.A());
  }

  public static class Output
  {
    public readonly record struct A;
  }
}
