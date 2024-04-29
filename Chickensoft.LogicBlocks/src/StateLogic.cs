namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// Logic block state. Inherit from this class to create a base state for a
/// logic block.
/// </summary>
/// <typeparam name="TState">State type inheriting from this record.</typeparam>
public abstract record StateLogic<TState> : StateBase
  where TState : StateLogic<TState> {
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

  /// <summary>
  /// Runs all of the registered entrance callbacks for the state.
  /// </summary>
  /// <param name="previous">Previous state, if any.</param>
  public void Enter(TState? previous = default) =>
    CallOnEnterCallbacks(previous, this as TState);

  /// <summary>
  /// Runs all of the registered exit callbacks for the state.
  /// </summary>
  /// <param name="next">Next state, if any.</param>
  public void Exit(TState? next = default) =>
    CallOnExitCallbacks(this as TState, next);

  /// <summary>
  /// Defines a transition to a state stored on the logic block's blackboard.
  /// </summary>
  /// <typeparam name="TStateType">Type of state to transition to.</typeparam>
  protected LogicBlock<TState>.Transition To<TStateType>()
    where TStateType : TState => new(Context.Get<TStateType>());

  /// <summary>Defines a self-transition.</summary>
  protected LogicBlock<TState>.Transition ToSelf() => new((this as TState)!);

  /// <summary>
  /// Adds an input value to the logic block's internal input queue and
  /// returns the current state.
  /// </summary>
  /// <param name="input">Input to process.</param>
  /// <typeparam name="TInputType">Type of the input.</typeparam>
  protected void Input<TInputType>(in TInputType input)
    where TInputType : struct => Context.Input(input);

  /// <summary>
  /// Produces a logic block output value.
  /// </summary>
  /// <typeparam name="TOutputType">Type of output to produce.</typeparam>
  /// <param name="output">Output value.</param>
  protected void Output<TOutputType>(in TOutputType output)
    where TOutputType : struct => Context.Output(output);

  /// <summary>
  /// Gets data from the blackboard.
  /// </summary>
  /// <typeparam name="TData">The type of data to retrieve.</typeparam>
  /// <exception cref="System.Collections.Generic.KeyNotFoundException" />
  protected TData Get<TData>() where TData : class => Context.Get<TData>();

  /// <summary>
  /// Adds an error to a logic block. Errors are immediately processed by the
  /// logic block's <see cref="LogicBlock{TState}.HandleError(Exception)"/>
  /// callback.
  /// </summary>
  /// <param name="e">Exception to add.</param>
  protected void AddError(Exception e) => Context.AddError(e);

  private void CallOnEnterCallbacks(TState? previous, TState? next) {
    if (next is StateLogic<TState> nextLogic) {
      foreach (var onEnter in nextLogic.InternalState.EnterCallbacks) {
        if (onEnter.IsType(previous)) {
          // Already entered this state type.
          continue;
        }
        RunSafe(onEnter.Callback, previous);
      }
    }
  }

  private void CallOnExitCallbacks(TState? previous, TState? next) {
    if (previous is StateLogic<TState> previousLogic) {
      foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
        if (onExit.IsType(next)) {
          // Not actually leaving this state type.
          continue;
        }
        RunSafe(onExit.Callback, next);
      }
    }
  }

  private void RunSafe(
    Action<TState?> callback, TState? stateArg
  ) {
    try { callback(stateArg); }
    catch (Exception e) {
      if (InternalState.ContextAdapter.OnError is { } onError) {
        onError(e);
        return;
      }
      throw;
    }
  }
}
