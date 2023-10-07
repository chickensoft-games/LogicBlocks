namespace Chickensoft.LogicBlocks;

using System;

public abstract partial class Logic<TState, THandler, TInputReturn, TUpdate> {
  /// <summary>Logic block context provided to each logic block state.</summary>
  public interface IContext {
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
    TState Input<TInputType>(TInputType input) where TInputType : notnull;
    /// <summary>
    /// Produces a logic block output value.
    /// </summary>
    /// <param name="output">Output value.</param>
    void Output(in object output);
    /// <summary>
    /// Gets a value from the logic block's blackboard.
    /// </summary>
    /// <typeparam name="TDataType">Type of value to retrieve.</typeparam>
    /// <returns>The requested value.</returns>
    TDataType Get<TDataType>() where TDataType : notnull;
    /// <summary>
    /// Adds an error to a logic block. Errors are immediately processed by the
    /// logic block's <see cref="HandleError(Exception)"/> callback.
    /// </summary>
    /// <param name="e">Exception to add.</param>
    void AddError(Exception e);
  }

  /// <summary>Logic block context provided to each logic block state.</summary>
  internal readonly record struct Context : IContext {
    private Logic<TState, THandler, TInputReturn, TUpdate> Logic { get; }

    /// <summary>
    /// Creates a new logic block context for the given logic block.
    /// </summary>
    /// <param name="logic">Logic block.</param>
    public Context(Logic<TState, THandler, TInputReturn, TUpdate> logic) {
      Logic = logic;
    }

    /// <inheritdoc />
    public TState Input<TInputType>(TInputType input)
    where TInputType : notnull {
      Logic.Input(input);
      return Logic.Value;
    }

    /// <inheritdoc />
    public void Output(in object output) => Logic.OutputValue(in output);

    /// <inheritdoc />
    public TDataType Get<TDataType>() where TDataType : notnull =>
      Logic.Get<TDataType>();

    /// <inheritdoc />
    public void AddError(Exception e) => Logic.AddError(e);
  }
}
