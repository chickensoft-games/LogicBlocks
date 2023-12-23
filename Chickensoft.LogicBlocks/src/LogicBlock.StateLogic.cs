namespace Chickensoft.LogicBlocks;

using System;

public partial class LogicBlock<TState> {
  /// <summary>Logic block base state record implementation.</summary>
  public abstract record StateLogic : InternalSharedState, IStateLogic {
    /// <inheritdoc />
    public void Enter(
      TState? previous = default, Action<Exception>? onError = null
    ) => CallOnEnterCallbacks(previous, this as TState, onError);

    /// <inheritdoc />
    public void Exit(
      TState? next = default, Action<Exception>? onError = null
    ) => CallOnExitCallbacks(this as TState, next, onError);

    /// <inheritdoc />
    public void OnEnter<TStateType>(Action<TState?> handler)
      where TStateType : class, IStateLogic =>
        InternalState.EnterCallbacks.Enqueue(
          new(handler, (state) => state is TStateType)
        );

    /// <inheritdoc />
    public void OnExit<TStateType>(Action<TState?> handler)
      where TStateType : class, IStateLogic =>
        InternalState.ExitCallbacks.Push(
          new(handler, (state) => state is TStateType)
        );

    private void CallOnEnterCallbacks(
      TState? previous, TState? next, Action<Exception>? onError
    ) {
      if (next is StateLogic nextLogic) {
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
      if (previous is StateLogic previousLogic) {
        foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
          if (onExit.IsType(next)) {
            // Not actually leaving this state type.
            continue;
          }
          RunSafe(() => onExit.Callback(next), onError);
        }
      }
    }
  }
}
