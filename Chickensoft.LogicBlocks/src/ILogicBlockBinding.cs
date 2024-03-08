namespace Chickensoft.LogicBlocks;

using System;

internal interface ILogicBlockBinding<TState>
where TState : class, IStateLogic<TState> {
  internal void MonitorInput<TInputType>(in TInputType input)
    where TInputType : struct;
  internal void MonitorState(TState state);
  internal void MonitorOutput<TOutput>(in TOutput output)
    where TOutput : struct;
  internal void MonitorException(Exception exception);
}
