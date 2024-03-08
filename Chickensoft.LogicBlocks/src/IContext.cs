namespace Chickensoft.LogicBlocks;

using System;

/// <summary>Logic block context provided to each logic block state.</summary>
public interface IContext {
  /// <summary>
  /// Adds an input value to the logic block's internal input queue and
  /// returns the current state.
  /// <br />
  /// Instead, represent loading as a state or a property of a state while you
  /// add un-awaited inputs from a state.
  /// </summary>
  /// <param name="input">Input to process.</param>
  /// <typeparam name="TInputType">Type of the input.</typeparam>
  void Input<TInputType>(in TInputType input) where TInputType : struct;

  /// <summary>
  /// Produces a logic block output value.
  /// </summary>
  /// <typeparam name="TOutputType">Type of output to produce.</typeparam>
  /// <param name="output">Output value.</param>
  void Output<TOutputType>(in TOutputType output) where TOutputType : struct;

  /// <summary>
  /// Gets a value from the logic block's blackboard.
  /// </summary>
  /// <typeparam name="TDataType">Type of value to retrieve.</typeparam>
  /// <returns>The requested value.</returns>
  TDataType Get<TDataType>() where TDataType : class;

  /// <summary>
  /// Adds an error to a logic block. Errors are immediately processed by the
  /// logic block's <see cref="LogicBlock{TState}.HandleError(Exception)"/>
  /// callback.
  /// </summary>
  /// <param name="e">Exception to add.</param>
  void AddError(Exception e);
}

internal interface IContextAdapter : IContext {
  IContext? Context { get; }

  void Adapt(IContext context);
  void Clear();
}
