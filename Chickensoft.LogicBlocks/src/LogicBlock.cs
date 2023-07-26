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
  /// The context provided to the states of the logic block.
  /// </summary>
  public new Context Context { get; private set; } = default!;

  /// <summary>
  /// Whether or not the logic block is processing inputs.
  /// </summary>
  public override bool IsProcessing => _isProcessing;

  private bool _isProcessing;

  /// <inheritdoc />
  public sealed override TState GetInitialState() {
    Context = new(this);
    return GetInitialState(Context);
  }

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

      // Call OnExit callbacks for StateLogic states.
      if (previous is StateLogic previousLogic) {
        foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
          if (onExit.IsType(state)) {
            // Not actually leaving this state type.
            continue;
          }
          RunSafe(() => onExit.Callback(state));
        }
      }

      SetState(state);

      // Call OnEnter callbacks for StateLogic states.
      if (state is StateLogic stateLogic) {
        foreach (var onEnter in stateLogic.InternalState.EnterCallbacks) {
          if (onEnter.IsType(previous)) {
            // Already entered this state type.
            continue;
          }
          RunSafe(() => onEnter.Callback(previous));
        }
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
