namespace Chickensoft.LogicBlocks.Tests;

using Fixtures;
using Shouldly;

public class ClassWithNoParameterlessConstructor
{
  public string Value { get; }
  public ClassWithNoParameterlessConstructor(string value)
  {
    Value = value;
  }
}

public class StateTesterTest
{
  [Fact]
  public void GetReturnsExistingValue()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Set("hello");
    tester.Get<string>().ShouldBe("hello");
  }

  [Fact]
  public void GetAutoCreatesWithParameterlessConstructor()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    var result = tester.Get<List<int>>();

    result.ShouldNotBeNull();
    tester.Has<List<int>>().ShouldBeTrue();
  }

  [Fact]
  public void GetThrowsWhenNoParameterlessConstructor()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    Should.Throw<LogicBlockException>(
      tester.Get<ClassWithNoParameterlessConstructor>
    );
  }

  [Fact]
  public void HasReturnsTrueWhenSet()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Set("hello");
    tester.Has<string>().ShouldBeTrue();
  }

  [Fact]
  public void HasReturnsFalseWhenNotSet()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Has<string>().ShouldBeFalse();
  }

  [Fact]
  public void InputRecordsInput()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Input(new TestLogicBlockState.Input.TestInput());

    tester.Inputs.Count.ShouldBe(1);
  }

  [Fact]
  public void OutputRecordsOutput()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Output(new TestLogicBlockState.Output.TestOutput());

    tester.Outputs.Count.ShouldBe(1);
  }

  [Fact]
  public void PushPeekPop()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();
    var pushed = typeof(TestLogicBlockState);

    tester.Push(pushed);
    tester.Peek().ShouldBe(pushed);
    tester.Pop().ShouldBe(pushed);
    tester.Peek().ShouldBeNull();
  }

  [Fact]
  public void PopReturnsNullWhenEmpty()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Pop().ShouldBeNull();
  }

  [Fact]
  public void ClearHistoryWorks()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Push(typeof(TestLogicBlockState));
    tester.ClearHistory();
    tester.History.Count.ShouldBe(0);
  }

  [Fact]
  public void HasHistoryReturnsFalseWhenEmpty()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.HasHistory.ShouldBeFalse();
  }

  [Fact]
  public void HasHistoryReturnsTrueAfterPush()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Push(typeof(TestLogicBlockState));
    tester.HasHistory.ShouldBeTrue();
  }

  [Fact]
  public void ResetClearsInputsOutputsHistory()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Input(new TestLogicBlockState.Input.TestInput());
    tester.Output(new TestLogicBlockState.Output.TestOutput());
    tester.Push(typeof(TestLogicBlockState));

    tester.Reset();

    tester.Inputs.Count.ShouldBe(0);
    tester.Outputs.Count.ShouldBe(0);
    tester.History.Count.ShouldBe(0);
  }

  [Fact]
  public void ClearResetsAndClearsBlackboard()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.Set("hello");
    tester.Input(new TestLogicBlockState.Input.TestInput());

    tester.Clear();

    tester.Has<string>().ShouldBeFalse();
    tester.Inputs.Count.ShouldBe(0);
  }

  [Fact]
  public void IsAttachedReturnsTrueOnStateTester()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.IsAttached.ShouldBeTrue();
  }

  [Fact]
  public void AttachAndDetachAreNoOpsOnStateTester()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    // Should not throw
    tester.Attach(null!);
    tester.Detach();
    tester.IsAttached.ShouldBeTrue();
  }

  [Fact]
  public void StatusDefaultsToStarted()
  {
    var tester = new StateTester();

    tester.Status.ShouldBe(LogicBlockStatus.Started);
  }

  [Fact]
  public void StopSetsStatusToStopped()
  {
    var tester = new StateTester();

    tester.Stop();

    tester.Status.ShouldBe(LogicBlockStatus.Stopped);
  }

  [Fact]
  public void StartSetsStatusToStarted()
  {
    var tester = new StateTester();
    tester.Stop();

    tester.Start();

    tester.Status.ShouldBe(LogicBlockStatus.Started);
  }

  [Fact]
  public void DisposeSetsStatusToDisposed()
  {
    var tester = new StateTester();

    tester.Dispose();

    tester.Status.ShouldBe(LogicBlockStatus.Disposed);
  }

  [Fact]
  public void TaskForwardsToTaskTracker()
  {
    var tester = new StateTester();

    tester.Task.IsCompleted.ShouldBeTrue();
  }

  [Fact]
  public async Task TrackTaskForwardsToTaskTracker()
  {
    var tester = new StateTester();
    var tcs = new TaskCompletionSource<bool>();

    tester.TrackTask(tcs.Task);
    tester.Task.IsCompleted.ShouldBeFalse();

    tcs.SetResult(true);
    await tester.Task;
    tester.Task.IsCompleted.ShouldBeTrue();
  }
}
