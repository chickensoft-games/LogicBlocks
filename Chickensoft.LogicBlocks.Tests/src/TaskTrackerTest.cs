namespace Chickensoft.LogicBlocks.Tests;

using Shouldly;

public class TaskTrackerTest
{
  [Fact]
  public void TaskIsCompletedWhenNothingTracked()
  {
    var tracker = new TaskTracker();

    tracker.Task.IsCompleted.ShouldBeTrue();
  }

  [Fact]
  public void TaskIsPendingWhileTracked()
  {
    var tracker = new TaskTracker();
    var tcs = new TaskCompletionSource<bool>();

    tracker.TrackTask(tcs.Task);

    tracker.Task.IsCompleted.ShouldBeFalse();
  }

  [Fact]
  public async Task TaskCompletesWhenAllTasksFinish()
  {
    var tracker = new TaskTracker();
    var tcs1 = new TaskCompletionSource<bool>();
    var tcs2 = new TaskCompletionSource<bool>();

    tracker.TrackTask(tcs1.Task);
    tracker.TrackTask(tcs2.Task);

    tracker.Task.IsCompleted.ShouldBeFalse();

    tcs1.SetResult(true);
    tracker.Task.IsCompleted.ShouldBeFalse();

    tcs2.SetResult(true);
    await tracker.Task;
    tracker.Task.IsCompleted.ShouldBeTrue();
  }

  [Fact]
  public async Task ResetsForNextCycleAfterCompletion()
  {
    var tracker = new TaskTracker();
    var tcs1 = new TaskCompletionSource<bool>();

    tracker.TrackTask(tcs1.Task);
    tcs1.SetResult(true);
    await tracker.Task;

    // New cycle — fresh TCS
    var tcs2 = new TaskCompletionSource<bool>();
    tracker.TrackTask(tcs2.Task);
    tracker.Task.IsCompleted.ShouldBeFalse();

    tcs2.SetResult(true);
    await tracker.Task;
    tracker.Task.IsCompleted.ShouldBeTrue();
  }

  [Fact]
  public void TrackingAlreadyCompletedTaskIsNoop()
  {
    var tracker = new TaskTracker();

    tracker.TrackTask(Task.CompletedTask);

    tracker.Task.IsCompleted.ShouldBeTrue();
  }
}
