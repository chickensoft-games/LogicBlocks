namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using Chickensoft.Introspection;

[Introspective("internals_logic")]
[LogicBlock(typeof(State))]
public partial class InternalsLogic : LogicBlock<InternalsLogic.State> {
  [Introspective("internals_logic_state")]
  public partial record State : StateLogic<State> {
    public Action? OnAttachAction { get; init; }
    public Action? OnDetachAction { get; init; }

    public TDataType PublicGet<TDataType>() where TDataType : class =>
      Get<TDataType>();

    public State() {
      OnAttach(() => OnAttachAction?.Invoke());
      OnDetach(() => OnDetachAction?.Invoke());
    }
  }

  public static class Input;

  public static class Output;

  public override Transition GetInitialState() => To<State>();
}
