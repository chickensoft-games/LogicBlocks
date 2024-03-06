namespace Chickensoft.LogicBlocks;

using System;

public abstract partial class LogicBlock<TState> {
  /// <summary>
  /// Fake binding that allows bindings to be triggered manually. Makes testing
  /// objects that bind to logic blocks easier.
  /// </summary>
  public interface IFakeBinding : IBinding {
    /// <summary>
    /// Triggers bindings to run in response to a state change.
    /// </summary>
    /// <param name="state">State.</param>
    void SetState(TState state);
    /// <summary>
    /// Triggers bindings to run in response to a new input.
    /// </summary>
    /// <param name="input">Input.</param>
    void Input(object input);
    /// <summary>
    /// Triggers bindings to run in response to an output.
    /// </summary>
    /// <param name="output">Output.</param>
    void Output(object output);
    /// <summary>
    /// Triggers bindings to run in response to an error.
    /// </summary>
    /// <param name="error">Error.</param>
    void AddError(Exception error);
  }

  internal sealed class FakeBinding : BindingBase, IFakeBinding {
    internal FakeBinding() { }

    public void Input(object input) => InternalOnInput(input);
    public void SetState(TState state) => InternalOnState(state);
    public void Output(object output) => InternalOnOutput(output);
    public void AddError(Exception error) => InternalOnError(error);
  }
}
