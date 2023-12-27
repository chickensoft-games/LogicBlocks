namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// <para>
/// A synchronous logic block. Logic blocks are machines that process inputs
/// one-at-a-time, maintain a current state graph, and produce outputs.
/// </para>
/// <para>
/// Logic blocks are essentially statecharts that are created using the state
/// pattern. Each state is a self-contained record.
/// </para>
/// </summary>
/// <typeparam name="TState">State type.</typeparam>
public interface ILogicBlock<TState> :
ILogic<
  TState, Func<object, TState>, TState, Action<TState?>
> where TState : class, LogicBlock<TState>.IStateLogic {
  /// <summary>
  /// Starts the logic block by entering the current state. If the logic block
  /// hasn't initialized yet, this will create the initial state before entering
  /// it.
  /// </summary>
  void Start();

  /// <summary>
  /// Stops the logic block by exiting the current state. For best results,
  /// don't continue to give inputs to the logic block after stopping it.
  /// Otherwise, you might end up firing the exit handler of a state more than
  /// once.
  /// </summary>
  void Stop();
}

/// <summary>
/// <para>
/// A synchronous logic block. Logic blocks are machines that process inputs
/// one-at-a-time, maintain a current state graph, and produce outputs.
/// </para>
/// <para>
/// Logic blocks are essentially statecharts that are created using the state
/// pattern. Each state is a self-contained record.
/// </para>
/// </summary>
/// <typeparam name="TState">State type.</typeparam>
public abstract partial class LogicBlock<TState> :
Logic<
  TState, Func<object, TState>, TState, Action<TState?>
>, ILogicBlock<TState> where TState : class, LogicBlock<TState>.IStateLogic {
  /// <summary>
  /// Whether or not the logic block is processing inputs.
  /// </summary>
  public override bool IsProcessing => _isProcessing;

  private bool _isProcessing;

  /// <summary>Creates a new logic block.</summary>
  protected LogicBlock() { }

  /// <inheritdoc />
  public abstract override TState GetInitialState();

  /// <inheritdoc />
  public override TState Value => _value ?? AttachState();

  private TState AttachState() {
    _isProcessing = true;
    _value = GetInitialState();
    _value.Attach(Context);
    _isProcessing = false;
    return Process();
  }

  internal override TState Process() {
    if (_value is null) {
      // No state yet.
      return AttachState(); // Calls Process() again.
    }

    if (IsProcessing) {
      return Value;
    }

    _isProcessing = true;

    while (GetNextInput(out var pendingInput)) {
      var handler = pendingInput.GetHandler();
      var input = pendingInput.Input;

      // Get next state.
      var state = handler(input);

      AnnounceInput(input);

      if (!CanChangeState(state)) {
        // The only time we can't change states is if the new state is
        // equivalent to the old state (determined by the default equality
        // comparer)
        continue;
      }

      var previous = Value;

      // Exit the previous state.
      previous.Exit(state, AddError);

      previous.Detach();

      SetState(state);

      state.Attach(Context);

      // Enter the next state.
      state.Enter(previous, AddError);

      FinalizeStateChange(state);
    }

    _isProcessing = false;

    return Value;
  }

  /// <inheritdoc />
  public void Start() =>
    Value.Enter(previous: null, onError: (e) => AddError(e));

  /// <inheritdoc />
  public void Stop() => Value.Exit(next: null, onError: (e) => AddError(e));

  internal override Func<object, TState> GetInputHandler<TInputType>()
    => (input) => {
      if (Value is IGet<TInputType> stateWithHandler) {
        return RunSafe(() => stateWithHandler.On((TInputType)input), Value);
      }

      return Value;
    };

  private TState RunSafe(Func<TState> handler, TState fallback) {
    try { return handler(); }
    catch (Exception e) { AddError(e); }
    return fallback;
  }
}
