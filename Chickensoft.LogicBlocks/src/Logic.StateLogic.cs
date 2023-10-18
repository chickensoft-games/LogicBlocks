namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

public abstract partial class Logic<TState, THandler, TInputReturn, TUpdate> {
  /// <summary>Logic state.</summary>
  public interface ILogicState { }

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
