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
public interface ILogicBlock<TInput, TState, TOutput>
  : ILogic<
      TInput, TState, TOutput, Func<TInput, TState>, TState, Action<TState>
    >
  where TInput : notnull
  where TState : Logic<
      TInput, TState, TOutput, Func<TInput, TState>, TState, Action<TState>
    >.IStateLogic
  where TOutput : notnull {
  /// <summary>
  /// Returns the initial state of the logic block. Implementations must
  /// override this method to provide a valid initial state.
  /// </summary>
  /// <param name="context">Logic block context.</param>
  /// <returns>Initial state of the logic block.</returns>
  TState GetInitialState(Logic<
    TInput, TState, TOutput, Func<TInput, TState>, TState, Action<TState>
  >.IContext context);

  /// <summary>
  /// Calls the entrance callbacks for the initial state. Technically, you can
  /// call this whenever you'd like to re-trigger entrance callbacks for the
  /// current state, but it's not recommended.
  /// <br />
  /// Calling this is optional. LogicBlocks doesn't invoke the initial state's
  /// entrance callbacks by default since the <see cref="Logic{
  /// TInput, TState, TOutput, THandler, TInputReturn, TUpdate
  /// }.Value"/> property  must be synchronous and the side effects of the
  /// initial state are often redundant in typical use cases representing the
  /// default starting state.
  /// <br />
  /// This is provided for those times when you actually do need the side
  /// effects from the initial state's entrance callbacks.
  /// </summary>
  void Start();
}

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
  Logic<
    TInput, TState, TOutput, Func<TInput, TState>, TState, Action<TState>
  >,
  ILogicBlock<TInput, TState, TOutput> where TInput : notnull
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
  /// Creates a new utility for testing the enter and exit callbacks on a state.
  /// </summary>
  /// <param name="state">State to be tested.</param>
  /// <returns>A new state tester.</returns>
  public static IStateTester Test(TState state) =>
    new StateTester<TInput, TState, TOutput>(state);

  /// <summary>
  /// The context provided to the states of the logic block.
  /// </summary>
  public new IContext Context { get; }

  /// <summary>
  /// Whether or not the logic block is processing inputs.
  /// </summary>
  public override bool IsProcessing => _isProcessing;

  private bool _isProcessing;

  /// <summary>Creates a new logic block.</summary>
  protected LogicBlock() {
    Context = new Context(this);
  }

  /// <inheritdoc />
  public sealed override TState GetInitialState() => GetInitialState(Context);

  /// <inheritdoc />
  public abstract TState GetInitialState(IContext context);

  /// <inheritdoc />
  public void Start() {
    if (IsProcessing) {
      return;
    }

    CallOnEnterCallbacks(default!, Value);
  }

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
      CallOnExitCallbacks(previous, state);

      SetState(state);

      // Call OnEnter callbacks for StateLogic states.
      CallOnEnterCallbacks(previous, state);

      FinalizeStateChange(state);
    }

    _isProcessing = false;

    return Value;
  }

  internal void CallOnExitCallbacks(TState previous, TState next) {
    if (previous is StateLogic previousLogic) {
      foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
        if (onExit.IsType(next)) {
          // Not actually leaving this state type.
          continue;
        }
        RunSafe(() => onExit.Callback(next));
      }
    }
  }

  internal void CallOnEnterCallbacks(TState previous, TState next) {
    if (next is StateLogic nextStateLogic) {
      foreach (var onEnter in nextStateLogic.InternalState.EnterCallbacks) {
        if (onEnter.IsType(previous)) {
          // Already entered this state type.
          continue;
        }
        RunSafe(() => onEnter.Callback(previous));
      }
    }
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
