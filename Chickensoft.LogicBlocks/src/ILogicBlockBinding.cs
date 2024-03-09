namespace Chickensoft.LogicBlocks;

using System;

/// <summary>Represents a logic block binding.</summary>
/// <typeparam name="TState">Logic block state type.</typeparam>
public interface ILogicBlockBinding<TState>
where TState : class, IStateLogic<TState> {
  internal void MonitorInput<TInputType>(in TInputType input)
    where TInputType : struct;
  internal void MonitorState(TState state);
  internal void MonitorOutput<TOutput>(in TOutput output)
    where TOutput : struct;
  internal void MonitorException(Exception exception);
}
