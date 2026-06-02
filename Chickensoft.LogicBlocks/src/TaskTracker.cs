namespace Chickensoft.LogicBlocks;

public interface ITaskTracker
{
  Task Task { get; }
  void TrackTask(Task task);
}

internal class TaskTracker : ITaskTracker
{
  private readonly object _lock = new();
  private int _pending;
  private TaskCompletionSource<bool> _completer = CreateCompleter();

  public Task Task
  {
    get
    {
      lock (_lock)
      {
        return _pending == 0 ? Task.CompletedTask : _completer.Task;
      }
    }
  }

  public void TrackTask(Task task)
  {
    if (task.IsCompleted)
    {
      return;
    }

    lock (_lock)
    {
      if (_pending == 0 && _completer.Task.IsCompleted)
      {
        _completer = CreateCompleter();
      }

      _pending++;
    }

    task.ContinueWith(
      OnTaskCompleted, TaskContinuationOptions.ExecuteSynchronously
    );
  }

  private void OnTaskCompleted(Task _)
  {
    lock (_lock)
    {
      _pending--;
      if (_pending == 0)
      {
        _completer.SetResult(true);
      }
    }
  }

  private static TaskCompletionSource<bool> CreateCompleter() =>
      new(TaskCreationOptions.RunContinuationsAsynchronously);
}
