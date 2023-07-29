namespace Chickensoft.LogicBlocks;

using System;

public abstract partial class Logic<
  TInput, TState, TOutput, THandler, TInputReturn, TUpdate
> {
  /// <summary>Logic block context provided to each logic block state.</summary>
  public readonly record struct Context {
    private Logic<
      TInput, TState, TOutput, THandler, TInputReturn, TUpdate
    > Logic { get; }

    /// <summary>
    /// Creates a new logic block context for the given logic block.
    /// </summary>
    /// <param name="logic">Logic block.</param>
    public Context(Logic<
      TInput, TState, TOutput, THandler, TInputReturn, TUpdate
    > logic) {
      Logic = logic;
    }

    /// <summary>
    /// Adds an input value to the logic block's internal input queue and
    /// returns the current state.
    /// <br />
    /// Async logic blocks cannot await an input they add from a state. If they
    /// did, input processing would hang forever in a type of input processing
    /// deadlock.
    /// <br />
    /// Instead, represent loading as a state or a property of a state while you
    /// add un-awaited inputs from a state.
    /// </summary>
    /// <param name="input">Input to process.</param>
    /// <typeparam name="TInputType">Type of the input.</typeparam>
    /// <returns>Logic block input return value.</returns>
    public TState Input<TInputType>(TInputType input)
      where TInputType : TInput {
      Logic.Input(input);
      return Logic.Value;
    }

    /// <summary>
    /// Produces a logic block output value.
    /// </summary>
    /// <param name="output">Output value.</param>
    public void Output(TOutput output) => Logic.OutputValue(output);

    /// <summary>
    /// Gets a value from the logic block's blackboard.
    /// </summary>
    /// <typeparam name="TDataType">Type of value to retrieve.</typeparam>
    /// <returns>The requested value.</returns>
    public TDataType Get<TDataType>() where TDataType : notnull =>
      Logic.Get<TDataType>();

    /// <summary>
    /// Adds an error to a logic block. Errors are immediately processed by the
    /// logic block's <see cref="HandleError(Exception)"/> callback.
    /// </summary>
    /// <param name="e">Exception to add.</param>
    public void AddError(Exception e) => Logic.AddError(e);
  }
}
