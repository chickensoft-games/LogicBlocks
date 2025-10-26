namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// State entrance and exit registration extensions.
/// </summary>
public static class StateLogicExtensions
{
  /// <summary>
  /// Adds a callback that will be invoked when the state is entered. The
  /// callback will receive the previous state as an argument.
  /// <br />
  /// Each class in an inheritance hierarchy can register callbacks and they
  /// will be invoked in the order they were registered, base class to most
  /// derived class. This ordering matches the order in which entrance
  /// callbacks should be invoked in a statechart.
  /// </summary>
  /// <typeparam name="TDerivedState">Derived state type that would be entered.
  /// </typeparam>
  /// <param name="state">State to add the callback to.</param>
  /// <param name="handler">Callback to be invoked when the state is entered.
  /// </param>
  public static void OnEnter<TDerivedState>(
    this TDerivedState state,
    Action handler
  )
  where TDerivedState : StateBase =>
    state.OnEnter<TDerivedState>((_) => handler());

  /// <summary>
  /// Adds a callback that will be invoked when the state is entered. The
  /// callback will receive the previous state as an argument.
  /// <br />
  /// Each class in an inheritance hierarchy can register callbacks and they
  /// will be invoked in the order they were registered, base class to most
  /// derived class. This ordering matches the order in which entrance
  /// callbacks should be invoked in a statechart.
  /// </summary>
  /// <typeparam name="TBaseState">Base state type.</typeparam>
  /// <typeparam name="TDerivedState">Derived state type that would be entered.
  /// </typeparam>
  /// <param name="state">State to add the callback to.</param>
  /// <param name="handler">Callback to be invoked when the state is entered.
  /// Receives the previous state, if any, as an argument.</param>
  public static void OnEnter<TBaseState, TDerivedState>(
    this TDerivedState state,
    Action<TBaseState?> handler
  )
  where TBaseState : StateBase
  where TDerivedState : StateBase, TBaseState =>
    state.OnEnter<TDerivedState>((previous) => handler(previous as TBaseState));

  /// <summary>
  /// Adds a callback that will be invoked when the state is exited. The
  /// callback will receive the previous state as an argument.
  /// <br />
  /// Each class in an inheritance hierarchy can register callbacks and they
  /// will be invoked in the opposite order they were registered, most
  /// derived class to base class. This ordering matches the order in which
  /// exit callbacks should be invoked in a statechart.
  /// </summary>
  /// <typeparam name="TDerivedState">Derived state type that would be exited.
  /// </typeparam>
  /// <param name="state">State to add the callback to.</param>
  /// <param name="handler">Callback to be invoked when the state is exited.
  /// </param>
  public static void OnExit<TDerivedState>(
    this TDerivedState state,
    Action handler
  )
  where TDerivedState : StateBase =>
    state.OnExit<TDerivedState>((_) => handler());

  /// <summary>
  /// Adds a callback that will be invoked when the state is exited. The
  /// callback will receive the previous state as an argument.
  /// <br />
  /// Each class in an inheritance hierarchy can register callbacks and they
  /// will be invoked in the opposite order they were registered, most
  /// derived class to base class. This ordering matches the order in which
  /// exit callbacks should be invoked in a statechart.
  /// </summary>
  /// <typeparam name="TBaseState">Base state type.</typeparam>
  /// <typeparam name="TDerivedState">Derived state type that would be exited.
  /// </typeparam>
  /// <param name="state">State to add the callback to.</param>
  /// <param name="handler">Callback to be invoked when the state is exited.
  /// Receives the next state, if any, as an argument.</param>
  public static void OnExit<TBaseState, TDerivedState>(
    this TDerivedState state,
    Action<TBaseState?> handler
  )
  where TBaseState : StateBase
  where TDerivedState : StateBase, TBaseState =>
    state.OnExit<TDerivedState>((next) => handler(next as TBaseState));
}
