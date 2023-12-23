namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class InternalsLogic : LogicBlock<InternalsLogic.State> {
  public record State : StateLogic {
    public Action? OnAttachAction { get; init; }
    public Action? OnDetachAction { get; init; }

    public TDataType PublicGet<TDataType>() where TDataType : notnull =>
      Get<TDataType>();

    public State() {
      OnAttach(() => OnAttachAction?.Invoke());
      OnDetach(() => OnDetachAction?.Invoke());
    }
  }

  public static class Input { }

  public static class Output { }

  public override State GetInitialState() => new();
}
