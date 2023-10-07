namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

public abstract partial class Logic<TState, THandler, TInputReturn, TUpdate> {
  /// <summary>
  /// Logic block state interface. All states used with a logic block must
  /// implement this interface.
  /// </summary>
  public interface IStateLogic {
    /// <summary>Logic block context.</summary>
    IContext Context { get; }

    /// <summary>Internal state used by LogicBlocks.</summary>
    internal StateLogicState InternalState { get; }

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
    void OnEnter<TStateType>(TUpdate handler) where TStateType : IStateLogic;

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
    void OnExit<TStateType>(TUpdate handler) where TStateType : IStateLogic;

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
    public void RegisterOnEnterCallback<TStateType>(TUpdate handler)
      where TStateType : IStateLogic =>
        InternalState.EnterCallbacks.Enqueue(
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
    public void RegisterOnExitCallback<TStateType>(TUpdate handler)
      where TStateType : IStateLogic =>
        InternalState.ExitCallbacks.Push(
          new(handler, (state) => state is TStateType)
        );
  }

  /// <summary>
  /// Internal state stored in each logic block state. This is used to store
  /// entrance and exit callbacks without tripping up equality checking.
  /// </summary>
  public readonly struct StateLogicState {
    /// <summary>
    /// Callbacks to be invoked when the state is entered.
    /// </summary>
    internal Queue<UpdateCallback> EnterCallbacks { get; }

    /// <summary>
    /// Callbacks to be invoked when the state is exited.
    /// </summary>
    internal Stack<UpdateCallback> ExitCallbacks { get; }

    /// <summary>Creates a new state logic internal state.</summary>
    public StateLogicState() {
      EnterCallbacks = new();
      ExitCallbacks = new();
    }

    // We don't want state logic states to be compared, so we make them
    // always equal to whatever other state logic state they are compared to.
    // This prevents issues where two seemingly equivalent states are not
    // deemed equivalent because their callbacks are different.

    /// <inheritdoc />
    public override bool Equals(object obj) => true;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
      EnterCallbacks,
      ExitCallbacks
    );
  }
}
