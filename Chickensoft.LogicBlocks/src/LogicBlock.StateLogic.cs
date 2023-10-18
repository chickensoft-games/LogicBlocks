namespace Chickensoft.LogicBlocks;

using System;

public abstract partial class LogicBlock<TState> {
  /// <summary>
  /// Logic block state interface. All states used with a logic block must
  /// implement this interface.
  /// </summary>
  public interface IStateLogic : ILogicState {
    /// <summary>Logic block context.</summary>
    IContext Context { get; }

    /// <summary>Internal state used by LogicBlocks.</summary>
    public StateLogicState InternalState { get; }

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
    void OnEnter<TStateType>(Action<TState?> handler)
      where TStateType : class, IStateLogic;

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
    void OnExit<TStateType>(Action<TState?> handler)
      where TStateType : class, IStateLogic;

    /// <summary>
    /// Runs all of the registered entrance callbacks for the state.
    /// </summary>
    /// <param name="previous">Previous state, if any.</param>
    /// <param name="onError">Error callback, if any.</param>
    void Enter(
      TState? previous = default, Action<Exception>? onError = null
    );

    /// <summary>
    /// Runs all of the registered exit callbacks for the state.
    /// </summary>
    /// <param name="next">Next state, if any.</param>
    /// <param name="onError">Error callback, if any.</param>
    void Exit(
      TState? next = default, Action<Exception>? onError = null
    );
  }
}
