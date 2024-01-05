namespace Chickensoft.LogicBlocks;

using System;

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
  void Input<TInputType>(TInputType input) where TInputType : notnull;

  /// <summary>
  /// Produces a logic block output value.
  /// </summary>
  /// <param name="output">Output value.</param>
  void Output<T>(in T output) where T : struct;

  /// <summary>
  /// Gets a value from the logic block's blackboard.
  /// </summary>
  /// <typeparam name="TDataType">Type of value to retrieve.</typeparam>
  /// <returns>The requested value.</returns>
  TDataType Get<TDataType>() where TDataType : notnull;

  /// <summary>
  /// Adds an error to a logic block. Errors are immediately processed by the
  /// logic block's <see cref="Logic{
  ///   TState, THandler, TInputReturn, TUpdate
  /// }.HandleError(Exception)"/> callback.
  /// </summary>
  /// <param name="e">Exception to add.</param>
  void AddError(Exception e);
}
