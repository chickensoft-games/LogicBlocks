namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// Logic block state interface. Inherit from <see cref="StateLogic{TState}" />
/// to create compatible logic block state.
/// </summary>
/// <typeparam name="TState">State type implementing this interface.</typeparam>
public interface IStateLogic<TState> : IStateBase
  where TState : class, IStateLogic<TState> {
  /// <summary>
  /// Runs all of the registered entrance callbacks for the state.
  /// </summary>
  /// <param name="previous">Previous state, if any.</param>
  /// <param name="onError">Error callback, if any.</param>
  void Enter(TState? previous = default, Action<Exception>? onError = null);

  /// <summary>
  /// Runs all of the registered exit callbacks for the state.
  /// </summary>
  /// <param name="next">Next state, if any.</param>
  /// <param name="onError">Error callback, if any.</param>
  void Exit(TState? next = default, Action<Exception>? onError = null);
}

/// <summary>
/// Logic block state. Inherit from this class to create a base state for a
/// logic block.
/// </summary>
/// <typeparam name="TState">State type inheriting from this record.</typeparam>
public abstract record StateLogic<TState> : StateBase, IStateLogic<TState>
  where TState : class, IStateLogic<TState> {
  /// <summary>
  /// Logic block state. Inherit from this class to create a base state for a
  /// logic block.
  /// </summary>
  protected StateLogic() : base(new LogicBlock<TState>.ContextAdapter()) { }

  /// <inheritdoc />
  internal override void OnEnter<TDerivedState>(Action<object?> handler)
    => InternalState.EnterCallbacks.Enqueue(
      new((obj) => handler(obj as TState), (state) => state is TDerivedState)
    );

  /// <inheritdoc />
  internal override void OnExit<TDerivedState>(Action<object?> handler)
    => InternalState.ExitCallbacks.Push(
      new((obj) => handler(obj as TState), (state) => state is TDerivedState)
    );

  /// <inheritdoc />
  public void Enter(
    TState? previous = default, Action<Exception>? onError = null
  ) => CallOnEnterCallbacks(previous, this as TState, onError);

  /// <inheritdoc />
  public void Exit(
    TState? next = default, Action<Exception>? onError = null
  ) => CallOnExitCallbacks(this as TState, next, onError);

  /// <summary>
  /// Gets data from the blackboard.
  /// </summary>
  /// <typeparam name="TData">The type of data to retrieve.</typeparam>
  /// <exception cref="System.Collections.Generic.KeyNotFoundException" />
  protected TData Get<TData>() where TData : class =>
    Context.Get<TData>();

  private void CallOnEnterCallbacks(
    TState? previous, TState? next, Action<Exception>? onError
  ) {
    if (next is StateLogic<TState> nextLogic) {
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
    if (previous is StateLogic<TState> previousLogic) {
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
