namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using Chickensoft.Introspection;

[Meta, LogicBlock(typeof(State))]
public partial class AbstractTransitionBlock : LogicBlock<AbstractTransitionBlock.State>
{
  public override Transition GetInitialState() => To<State.A>();

  public static class Input
  {
    public readonly record struct Signal;
  }

  public abstract record State : StateLogic<State>
  {
    public record A : State, IGet<Input.Signal>
    {
      // throws at runtime since you can't transition to an abstract state
      public Transition On(in Input.Signal input) => To<State>();
    }

    public record B : State;
  }

  protected override void HandleError(Exception e) => throw e;
}
