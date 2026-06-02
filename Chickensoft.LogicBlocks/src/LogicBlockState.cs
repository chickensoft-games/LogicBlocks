namespace Chickensoft.LogicBlocks;

using System.Runtime.CompilerServices;

public abstract record LogicBlockState : IStateful
{
  internal InternalState InternalState { get; } = new();

  #region IStateful

  public bool IsAttached => InternalState.IsAttached;
  public LogicBlockStatus Status => InternalState.Status;

  public void Attach(ILogicBlock logic)
  {
    if (InternalState is IStateful context)
    {
      // in production code, the context should have a reference to the
      // state machine when it is the active state
      context.Attach(logic);
    }
  }

  public void Detach()
  {
    if (InternalState is IStateful context)
    {
      context.Detach();
    }
  }

  public void Input<TInput>(in TInput input)
    where TInput : struct => InternalState.Input(input);

  public void Output<TOutput>(in TOutput output)
    where TOutput : struct => InternalState.Output(output);

  public TData Get<TData>()
    where TData : class => InternalState.Get<TData>();

  public bool Has<T>()
    where T : class => InternalState.Has<T>();

  public bool HasHistory => InternalState.HasHistory;
  public void Push(Type type) => InternalState.Push(type);
  public void Push() => InternalState.Push(GetType());
  public Type? Peek() => InternalState.Peek();
  public Type? Pop() => InternalState.Pop();
  public void ClearHistory() => InternalState.ClearHistory();

  public Task Task => InternalState.Task;
  public void TrackTask(Task task) => InternalState.TrackTask(task);

  #endregion Stateful

  /// <summary>
  /// Defines a transition to a state stored on the logic block's blackboard.
  /// </summary>
  /// <typeparam name="TState">Type of state to transition to.</typeparam>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected internal Type To<TState>()
    where TState : LogicBlockState => typeof(TState);

  /// <summary>Defines a self-transition.</summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected internal Type ToSelf() => GetType();

  protected internal StatefulTask<TValue> Async<TValue>(Task<TValue> task)
  {
    return new StatefulTask<TValue>(task, InternalState.Context);
  }

  public virtual StateTester Test() => InternalState.Test();

  #region EnterAndExit

  public void Enter(LogicBlockState? previous = null)
  {
    if (!InternalState.IsAttached)
    {
      throw new LogicBlockException(
        "Cannot enter a state that is not attached to the state machine. " +
        "If you are unit testing a state in isolation, be sure to call " +
        "`var tester = state.Test()` first."
      );
    }

    InternalState.CallOnEnterCallbacks(previous, this);
  }

  public void Exit(LogicBlockState? next = null)
  {
    if (!InternalState.IsAttached)
    {
      throw new LogicBlockException(
        "Cannot exit a state that is not attached to the state machine. " +
        "If you are unit testing a state in isolation, be sure to call " +
        "`var tester = state.Test()` first."
      );
    }

    InternalState.CallOnExitCallbacks(this, next);
  }

  #endregion EnterAndExit
}
