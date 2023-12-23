namespace Chickensoft.LogicBlocks.Generator.Tests;

using System;

[StateMachine]
public partial class PartialLogic : LogicBlock<PartialLogic.State> {
  public override State GetInitialState() => throw new NotImplementedException();

  public static class Input {
    public readonly record struct One;
  }
  public abstract partial record State : StateLogic;
  public static class Output {
    public readonly record struct OutputA;
    public readonly record struct OutputEnterA;
    public readonly record struct OutputExitA;
    public readonly record struct OutputSomething;
  }
}
