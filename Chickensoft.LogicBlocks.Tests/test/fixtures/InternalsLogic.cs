namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;

[LogicBlock(typeof(State))]
public class InternalsLogic : LogicBlock<InternalsLogic.State> {
  public record State : StateLogic<State> {
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

  public override State GetInitialState() => new();
}
