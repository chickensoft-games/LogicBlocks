namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// <para>
/// A synchronous logic block. Logic blocks are machines that process inputs
/// one-at-a-time, maintain a current state graph, and produce outputs.
/// </para>
/// <para>
/// Logic blocks are essentially statecharts that are created using the state
/// pattern. Each state is a self-contained class, record, or struct that
/// implements <see cref="Logic{TInput, TState, TOutput, THandler,
/// TInputReturn, TUpdate}.IStateLogic"/>.
/// </para>
/// </summary>
/// <typeparam name="TInput">Input type.</typeparam>
/// <typeparam name="TState">State type.</typeparam>
/// <typeparam name="TOutput">Output type.</typeparam>
public abstract partial class LogicBlock<TInput, TState, TOutput> :
  Logic<TInput, TState, TOutput, Func<TInput, TState>, TState, Action<TState>>
  where TInput : notnull
  where TState : Logic<
    TInput,
    TState,
    TOutput,
    Func<TInput, TState>,
    TState,
    Action<TState>
  >.IStateLogic
  where TOutput : notnull {
  /// <summary>
  /// Whether or not the logic block is processing inputs.
  /// </summary>
  public override bool IsProcessing => _isProcessing;

  private bool _isProcessing;

  /// <inheritdoc />
  public sealed override TState GetInitialState() =>
    GetInitialState(new Context(this));

  /// <summary>
  /// Returns the initial state of the logic block. Implementations must
  /// override this method to provide a valid initial state.
  /// </summary>
  /// <param name="context">Logic block context.</param>
  /// <returns>Initial state of the logic block.</returns>
  public abstract TState GetInitialState(Context context);

  internal override TState Process() {
    if (IsProcessing) {
      return Value;
    }

    _isProcessing = true;

    while (GetNextInput(out var pendingInput)) {
      var handler = pendingInput.GetHandler();
      var input = pendingInput.Input;

      // Save previous enter callbacks in case we can't change states and need
      // to restore them. This does it without allocating a new list.
      Flip();

      // Get next state. This triggers the next state to register its
      // OnEnter and OnExit callbacks.
      var state = handler(input);

      AnnounceInput(input);

      if (!CanChangeState(state)) {
        // Don't save registered callbacks from the state we couldn't change to.
        ExitCallbacks.Clear();
        EnterCallbacks.Clear();
        Flip(); // Restore previous enter/exit callbacks.
        continue;
      }

      // Restore previous enter/exit callbacks.
      Flip();

      var previous = Value;

      // Call previously registered OnExit callbacks.
      foreach (var onExit in ExitCallbacks) {
        if (onExit.IsType(state)) {
          // Not actually leaving this state type.
          continue;
        }
        RunSafe(() => onExit.Callback(state));
      }

      // Now that exit callbacks have run, clear them.
      ExitCallbacks.Clear();
      // The previous state already had its enter callbacks run.
      EnterCallbacks.Clear();

      SetState(state);

      // Use new state's enter callbacks right now.
      Flip();

      // Call newly registered OnEnter callbacks.
      foreach (var onEnter in EnterCallbacks) {
        if (onEnter.IsType(previous)) {
          // Already entered this state type.
          continue;
        }
        RunSafe(() => onEnter.Callback(previous));
      }

      FinalizeStateChange(state);
    }

    _isProcessing = false;

    return Value;
  }

  internal override Func<TInput, TState> GetInputHandler<TInputType>()
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

  private void RunSafe(Action callback) {
    try { callback(); }
    catch (Exception e) { AddError(e); }
  }
}
