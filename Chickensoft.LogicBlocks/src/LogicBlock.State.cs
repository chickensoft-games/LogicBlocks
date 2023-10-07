namespace Chickensoft.LogicBlocks;

using System;

public partial class LogicBlock<TState> {
  /// <summary>
  /// Logic block base state record. If you are using records for your logic
  /// block states, you may inherit from this record rather instead of
  /// implementing <see cref="Logic{
  ///   TState, THandler, TInputReturn, TUpdate
  /// }.IStateLogic"/> directly and storing a context
  /// in each state.
  /// </summary>
  public abstract record StateLogic : IStateLogic {
    /// <inheritdoc />
    public IContext Context { get; }

    StateLogicState IStateLogic.InternalState { get; } = new();

    /// <summary>
    /// Creates a new instance of the logic block base state record.
    /// </summary>
    /// <param name="context">Logic block context.</param>
    public StateLogic(IContext context) {
      Context = context;
    }

    /// <summary>
    /// Runs all of the registered entrance callbacks for the state.
    /// </summary>
    /// <param name="previous">Previous state, if any.</param>
    /// <param name="onError">Error callback, if any.</param>
    public void Enter(
      TState? previous = default, Action<Exception>? onError = null
    ) => CallOnEnterCallbacks(previous, this as TState, onError);

    /// <summary>
    /// Runs all of the registered exit callbacks for the state.
    /// </summary>
    /// <param name="next">Next state, if any.</param>
    /// <param name="onError">Error callback, if any.</param>
    public void Exit(
      TState? next = default, Action<Exception>? onError = null
    ) => CallOnExitCallbacks(this as TState, next, onError);

    /// <inheritdoc />
    public void OnEnter<TStateType>(Action<TState?> handler)
      where TStateType : IStateLogic =>
        (this as IStateLogic).RegisterOnEnterCallback<TStateType>(handler);

    /// <inheritdoc />
    public void OnExit<TStateType>(Action<TState?> handler)
      where TStateType : IStateLogic =>
        (this as IStateLogic).RegisterOnExitCallback<TStateType>(handler);

    private void CallOnEnterCallbacks(
      TState? previous, TState? next, Action<Exception>? onError
    ) {
      if (next is IStateLogic nextLogic) {
        foreach (var onEnter in nextLogic.InternalState.EnterCallbacks) {
          if (onEnter.IsType(previous)) {
            // Already entered this state type.
            continue;
          }
          RunSafe(() => onEnter.Callback(previous), onError);
        }
      }
    }

    private void CallOnExitCallbacks(
      TState? previous, TState? next, Action<Exception>? onError
    ) {
      if (previous is IStateLogic previousLogic) {
        foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
          if (onExit.IsType(next)) {
            // Not actually leaving this state type.
            continue;
          }
          RunSafe(() => onExit.Callback(next), onError);
        }
      }
    }

    private void RunSafe(Action callback, Action<Exception>? onError) {
      try { callback(); }
      catch (Exception e) {
        if (onError is Action<Exception> onErrorHandler) {
          onErrorHandler.Invoke(e);
          return;
        }
        throw;
      }
    }
  }
}
