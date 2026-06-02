namespace Chickensoft.LogicBlocks;

public readonly struct StatefulTask<TValue>
{
  public Task<TValue> Task { get; }
  public IStateful Stateful { get; }

  private readonly TaskScheduler _scheduler;

  internal StatefulTask(Task<TValue> task, IStateful stateful)
  {
    Task = task;
    Stateful = stateful;
    _scheduler = SynchronizationContext.Current is not null
      ? TaskScheduler.FromCurrentSynchronizationContext()
      : TaskScheduler.Current;
  }

  public StatefulTask<TValue> Input<TInput>(
    Func<TValue, TInput> toInput
  ) where TInput : struct
  {
    var stateful = Stateful;

    stateful.TrackTask(
      Task.ContinueWith(
        t =>
        {
          if (stateful.Status == LogicBlockStatus.Started)
          {
            stateful.Input(toInput(t.Result));
          }
        },
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnRanToCompletion,
        _scheduler
      )
    );

    return this;
  }

  public StatefulTask<TValue> ErrorInput<TInput>(
    Func<Exception, TInput> toInput
  ) where TInput : struct
  {
    var stateful = Stateful;

    stateful.TrackTask(
      Task.ContinueWith(
        t =>
        {
          if (stateful.Status == LogicBlockStatus.Started)
          {
            stateful.Input(toInput(t.Exception));
          }
        },
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted,
        _scheduler
      )
    );

    return this;
  }

  public StatefulTask<TValue> CanceledInput<TInput>(
    Func<TInput> toInput
  ) where TInput : struct
  {
    var stateful = Stateful;

    stateful.TrackTask(
      Task.ContinueWith(
        t =>
        {
          if (stateful.Status == LogicBlockStatus.Started)
          {
            stateful.Input(toInput());
          }
        },
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnCanceled,
        _scheduler
      )
    );

    return this;
  }
}
