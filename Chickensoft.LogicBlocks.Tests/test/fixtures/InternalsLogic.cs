namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using Chickensoft.Introspection;

[LogicBlock(typeof(State)), Meta]
public partial class InternalsLogic : LogicBlock<InternalsLogic.State>
{
  public partial record State : StateLogic<State>
  {
    public Action? OnAttachAction { get; init; }
    public Action? OnDetachAction { get; init; }

    public TDataType PublicGet<TDataType>() where TDataType : class =>
      Get<TDataType>();

    public State()
    {
      OnAttach(() => OnAttachAction?.Invoke());
      OnDetach(() => OnDetachAction?.Invoke());
    }
  }

  public static class Input;

  public static class Output;

  public override Transition GetInitialState() => To<State>();
}
