namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

public abstract partial class Logic<
  TInput, TState, TOutput, THandler, TInputReturn, TUpdate
> {
  /// <summary>
  /// Logic block state interface. All states used with a logic block must
  /// implement this interface.
  /// </summary>
  public interface IStateLogic {
    /// <summary>Logic block context.</summary>
    IContext Context { get; }
  }

  internal class StateLogicState {
    /// <summary>
    /// Callbacks to be invoked when the state is entered.
    /// </summary>
    internal Queue<UpdateCallback> EnterCallbacks { get; } = new();

    /// <summary>
    /// Callbacks to be invoked when the state is exited.
    /// </summary>
    internal Stack<UpdateCallback> ExitCallbacks { get; } = new();

    // We don't want state logic states to be compared, so we make them
    // always equal to whatever other state logic state they are compared to.
    // This prevents issues where two seemingly equivalent states are not
    // deemed equivalent because their callbacks are different.
    public override bool Equals(object obj) => true;

    public override int GetHashCode() => HashCode.Combine(
      EnterCallbacks,
      ExitCallbacks
    );
  }

  /// <summary>
  /// Logic block base state record. If you are using records for your logic
  /// block states, you may inherit from this record rather instead of
  /// implementing <see cref="IStateLogic"/> directly and storing a context
  /// in each state.
  /// </summary>
  public abstract record StateLogic : IStateLogic {
    /// <summary>Logic block context.</summary>
    public IContext Context { get; }

    internal StateLogicState InternalState { get; }

    /// <summary>
    /// Creates a new instance of the logic block base state record.
    /// </summary>
    /// <param name="context">Logic block context.</param>
    public StateLogic(IContext context) {
      Context = context;
      InternalState = new();
    }

    /// <summary>
    /// Adds a callback that will be invoked when the state is entered. The
    /// callback will receive the previous state as an argument.
    /// <br />
    /// Each class in an inheritance hierarchy can register callbacks and they
    /// will be invoked in the order they were registered, base class to most
    /// derived class. This ordering matches the order in which entrance
    /// callbacks should be invoked in a statechart.
    /// </summary>
    /// <typeparam name="TStateType">Type of the state that would be entered.
    /// </typeparam>
    /// <param name="handler">Callback to be invoked when the state is entered.
    /// </param>
    public void OnEnter<TStateType>(TUpdate handler)
      where TStateType : StateLogic => InternalState.EnterCallbacks.Enqueue(
      new(handler, (state) => state is TStateType)
    );

    /// <summary>
    /// Adds a callback that will be invoked when the state is exited. The
    /// callback will receive the next state as an argument.
    /// <br />
    /// Each class in an inheritance hierarchy can register callbacks and they
    /// will be invoked in the opposite order they were registered, most
    /// derived class to base class. This ordering matches the order in which
    /// exit callbacks should be invoked in a statechart.
    /// </summary>
    /// <typeparam name="TStateType">Type of the state that would be exited.
    /// </typeparam>
    /// <param name="handler">Callback to be invoked when the state is exited.
    /// </param>
    public void OnExit<TStateType>(TUpdate handler)
      where TStateType : StateLogic => InternalState.ExitCallbacks.Push(
      new(handler, (state) => state is TStateType)
    );
  }
}
