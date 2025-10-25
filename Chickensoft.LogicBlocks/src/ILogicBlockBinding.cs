namespace Chickensoft.LogicBlocks;

using System;

/// <summary>Represents a logic block binding.</summary>
/// <typeparam name="TState">Logic block state type.</typeparam>
public interface ILogicBlockBinding<TState>
where TState : StateLogic<TState>
{
  /// <summary>Called when the logic block receives an input.</summary>
  /// <param name="input">Input received.</param>
  /// <typeparam name="TInput">Type of the input.</typeparam>
  internal void MonitorInput<TInput>(in TInput input)
    where TInput : struct;

  /// <summary>Called when the logic block changes state.</summary>
  /// <param name="state">New state.</param>
  internal void MonitorState(TState state);

  /// <summary>Called when the logic block produces an output.</summary>
  /// <param name="output">Output received.</param>
  /// <typeparam name="TOutput">Type of the output.</typeparam>
  internal void MonitorOutput<TOutput>(in TOutput output)
    where TOutput : struct;

  /// <summary>Called when the logic block encounters an exception.</summary>
  /// <param name="exception">Exception encountered.</param>
  internal void MonitorException(Exception exception);
}
