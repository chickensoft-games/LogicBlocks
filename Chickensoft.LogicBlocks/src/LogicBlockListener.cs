namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// <para>
/// Logic block listener. Receives callbacks for inputs, states, outputs, and
/// exceptions that the logic block encounters.
/// </para>
/// <para>
/// For the sake of performance, LogicBlocks cannot be subscribed to with events
/// or observables. Instead, simply subclass this when you need to listen to
/// every input, state, output, and/or exception that a logic block encounters.
/// </para>
/// <para>
/// The generics are required on
/// <see cref="ReceiveInput{TInputType}(in TInputType)" />
/// and <see cref="ReceiveOutput{TOutputType}(in TOutputType)" /> to
/// prevent inputs and outputs from unnecessarily hitting the heap.
/// </para>
/// </summary>
/// <typeparam name="TState">State type.</typeparam>
public abstract class LogicBlockListenerBase<TState> :
ILogicBlockBinding<TState>, IDisposable
where TState : class, IStateLogic<TState> {
  /// <summary>
  /// <para>
  /// Creates a new logic block listener.
  /// </para>
  /// <para>
  /// A logic block listener receives callbacks for inputs, states, outputs, and
  /// exceptions that the logic block encounters.
  /// </para>
  /// </summary>
  protected LogicBlockListenerBase() { }

  /// <summary>
  /// Called whenever the logic block receives an input.
  /// </summary>
  /// <typeparam name="TInputType">Input type.</typeparam>
  /// <param name="input">Input object.</param>
  protected virtual void ReceiveInput<TInputType>(in TInputType input)
    where TInputType : struct { }

  /// <summary>
  /// Called whenever the logic block transitions to a new state.
  /// </summary>
  /// <param name="state">New state.</param>
  protected virtual void ReceiveState(TState state) { }

  /// <summary>
  /// Called whenever the logic block produces an output.
  /// </summary>
  /// <typeparam name="TOutputType">Output type.</typeparam>
  protected virtual void ReceiveOutput<TOutputType>(in TOutputType output)
    where TOutputType : struct { }

  /// <summary>
  /// Called whenever the logic block encounters an exception.
  /// </summary>
  /// <param name="e">Exception object.</param>
  protected virtual void ReceiveException(Exception e) { }

  void ILogicBlockBinding<TState>.MonitorInput<TInputType>(
    in TInputType input
  ) => ReceiveInput(in input);

  void ILogicBlockBinding<TState>.MonitorState(TState state) =>
    ReceiveState(state);

  void ILogicBlockBinding<TState>.MonitorOutput<TOutputType>(
    in TOutputType output
  ) => ReceiveOutput(in output);

  void ILogicBlockBinding<TState>.MonitorException(Exception exception) =>
    ReceiveException(exception);

  /// <summary>
  /// Override this method to perform custom cleanup for your listener. This is
  /// called when the listener is disposed. Be sure to call the base method so
  /// this can unsubscribe from the logic block its listening to.
  /// </summary>
  protected abstract void Cleanup();

  /// <inheritdoc />
  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  private void Dispose(bool disposing) {
    if (disposing) {
      Cleanup();
    }
  }

  /// <summary>Binding finalizer.</summary>
  ~LogicBlockListenerBase() {
    Dispose(false);
  }
}

/// <inheritdoc />
public class LogicBlockListener<TState> : LogicBlockListenerBase<TState>
where TState : class, IStateLogic<TState> {
  /// <summary>Logic block being listened to.</summary>
  public ILogicBlock<TState> LogicBlock { get; }

  /// <inheritdoc cref="LogicBlockListenerBase{TState}" />
  /// <param name="logicBlock">Logic block to listen to.</param>
  public LogicBlockListener(ILogicBlock<TState> logicBlock) {
    LogicBlock = logicBlock;
    LogicBlock.AddBinding(this);
  }

  /// <inheritdoc />
  protected override void Cleanup() => LogicBlock.RemoveBinding(this);
}
