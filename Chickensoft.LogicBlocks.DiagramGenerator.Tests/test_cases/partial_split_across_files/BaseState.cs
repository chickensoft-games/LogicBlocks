namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic
{
  public static class Input
  {
    public readonly record struct One;
  }

  [StateDiagram]
  public abstract partial record BaseState : LogicBlockState;

  public static class Output
  {
    public readonly record struct OutputA;

    public readonly record struct OutputEnterA;

    public readonly record struct OutputExitA;

    public readonly record struct OutputSomething;
  }
}
