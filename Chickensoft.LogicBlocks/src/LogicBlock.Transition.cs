namespace Chickensoft.LogicBlocks;

using System;
using System.Diagnostics.CodeAnalysis;

public abstract partial class LogicBlock<TState> {
  /// <summary>Represents a transition to a new state.</summary>
  public readonly struct Transition {
    /// <summary>State to transition to.</summary>
    public TState State { get; }

    /// <summary>Don't use.</summary>
    [ExcludeFromCodeCoverage]
    [Obsolete(
      "Do not instantiate transitions yourself. Use To<T> or ToSelf()",
      error: true
    )]
    public Transition() {
      throw new NotSupportedException(
        "Transition should not be instantiated without a state."
      );
    }

    /// <summary>Creates a new state transition.</summary>
    /// <param name="state"><inheritdoc cref="State" path="/summary" /></param>
    internal Transition(TState state) {
      State = state;
    }

    /// <summary>
    /// Performs an action on the state before transitioning to it.
    /// </summary>
    /// <param name="action">Action to perform.</param>
    /// <returns>The same transition.</returns>
    public Transition With(Action<TState> action) {
      action(State);
      return this;
    }
  }
}
