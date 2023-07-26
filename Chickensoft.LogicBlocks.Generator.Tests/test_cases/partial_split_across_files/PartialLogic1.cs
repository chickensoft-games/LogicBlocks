namespace Chickensoft.LogicBlocks.Generator.Tests;

using System;

[StateMachine]
public partial class PartialLogic :
  LogicBlock<PartialLogic.Input, PartialLogic.State, PartialLogic.Output> {
  public override State GetInitialState(Context context) =>
    throw new NotImplementedException();

  public abstract record Input {
    public record One : Input;
  }
  public abstract partial record State(Context Context) : StateLogic(Context);
  public abstract record Output {
    public record OutputA : Output;
    public record OutputEnterA : Output;
    public record OutputExitA : Output;
    public record OutputSomething : Output;
  }
}
