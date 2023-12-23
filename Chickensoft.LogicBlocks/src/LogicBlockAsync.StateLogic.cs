namespace Chickensoft.LogicBlocks;

using System;
using System.Threading.Tasks;

public partial class LogicBlockAsync<TState> {
  /// <summary>Logic block base state record implementation.</summary>
  public abstract record StateLogic : InternalSharedState, IStateLogic {
    /// <summary>
    /// Runs all of the registered entrance callbacks for the state.
    /// </summary>
    /// <param name="previous">Previous state, if any.</param>
    /// <param name="onError">Error callback, if any.</param>
    public Task Enter(
      TState? previous = default, Action<Exception>? onError = null
    ) => CallOnEnterCallbacks(previous, this as TState, onError);

    /// <summary>
    /// Runs all of the registered exit callbacks for the state.
    /// </summary>
    /// <param name="next">Next state, if any.</param>
    /// <param name="onError">Error callback, if any.</param>
    public Task Exit(
      TState? next = default, Action<Exception>? onError = null
    ) => CallOnExitCallbacks(this as TState, next, onError);

    /// <inheritdoc />
    public void OnEnter<TStateType>(Func<TState?, Task> handler)
      where TStateType : class, IStateLogic =>
        InternalState.EnterCallbacks.Enqueue(
          new(handler, (state) => state is TStateType)
        );

    /// <inheritdoc />
    public void OnExit<TStateType>(Func<TState?, Task> handler)
      where TStateType : class, IStateLogic =>
        InternalState.ExitCallbacks.Push(
          new(handler, (state) => state is TStateType)
        );

    private async Task CallOnEnterCallbacks(
      TState? previous, TState? next, Action<Exception>? onError
    ) {
      if (next is StateLogic nextLogic) {
        foreach (var onEnter in nextLogic.InternalState.EnterCallbacks) {
          if (onEnter.IsType(previous)) {
            // Already entered this state type.
            continue;
          }
          await RunSafe(() => onEnter.Callback(previous), onError);
        }
      }
    }

    private async Task CallOnExitCallbacks(
      TState? previous, TState? next, Action<Exception>? onError
    ) {
      if (previous is StateLogic previousLogic) {
        foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
          if (onExit.IsType(next)) {
            // Not actually leaving this state type.
            continue;
          }
          await RunSafe(() => onExit.Callback(next), onError);
        }
      }
    }

    private async Task RunSafe(
      Func<Task> callback, Action<Exception>? onError
    ) {
      try { await callback(); }
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
