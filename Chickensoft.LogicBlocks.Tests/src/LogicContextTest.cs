namespace Chickensoft.LogicBlocks.Tests;

using Collections;
using Fixtures;
using Moq;
using Shouldly;

public class LogicContextTest
{
  [Fact]
  public void LogicThrowsWhenDetached()
  {
    var ctx = new LogicContext();

    Should.Throw<LogicBlockException>(() => { _ = ctx.Logic; });
  }

  [Fact]
  public void AttachThrowsForNonLogicBlock()
  {
    var ctx = new LogicContext();

    Should.Throw<LogicBlockException>(
      () => ctx.Attach(new Mock<ILogicBlock>().Object)
    );
  }

  [Fact]
  public void AttachThrowsWhenAlreadyAttached()
  {
    var ctx = new LogicContext();
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);

    ctx.Attach(logic);

    Should.Throw<LogicBlockException>(() => ctx.Attach(logic));
  }

  [Fact]
  public void DetachSetsLogicToNull()
  {
    var ctx = new LogicContext();
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);

    ctx.Attach(logic);
    ctx.IsAttached.ShouldBeTrue();

    ctx.Detach();
    ctx.IsAttached.ShouldBeFalse();
  }

  [Fact]
  public void InputForwardsToLogicBlock()
  {
    var bb = new Blackboard();
    var state = new TestLogicBlockState();
    var inputReceived = false;
    state.OnInputAction = _ => inputReceived = true;
    bb.Set(state);
    using var logic = new TestLogicBlock(bb);
    logic.Start<TestLogicBlockState>();

    var ctx = new LogicContext();
    ctx.Attach(logic);
    ctx.Input(new TestLogicBlockState.Input.TestInput());

    inputReceived.ShouldBeTrue();
  }

  [Fact]
  public void GetForwardsToLogicBlock()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    bb.Set("hello from blackboard");
    using var logic = new TestLogicBlock(bb);

    var ctx = new LogicContext();
    ctx.Attach(logic);

    ctx.Get<string>().ShouldBe("hello from blackboard");
  }

  [Fact]
  public void HasForwardsToLogicBlock()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    bb.Set("hello");
    using var logic = new TestLogicBlock(bb);

    var ctx = new LogicContext();
    ctx.Attach(logic);

    ctx.Has<string>().ShouldBeTrue();
    ctx.Has<List<int>>().ShouldBeFalse();
  }

  [Fact]
  public void HistoryForwardsToLogicBlock()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);
    logic.Start<TestLogicBlockState>();

    var ctx = new LogicContext();
    ctx.Attach(logic);

    var pushed = typeof(TestLogicBlockState);
    ctx.Push(pushed);
    ctx.Peek().ShouldBe(pushed);
    ctx.Pop().ShouldBe(pushed);

    ctx.Push(pushed);
    ctx.ClearHistory();
    logic.History.Count.ShouldBe(0);
  }

  [Fact]
  public void TaskForwardsToLogicBlock()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);
    logic.Start<TestLogicBlockState>();

    var ctx = new LogicContext();
    ctx.Attach(logic);

    ctx.Task.ShouldBe(logic.Task);
  }

  [Fact]
  public async Task TrackForwardsToLogicBlock()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);
    logic.Start<TestLogicBlockState>();

    var ctx = new LogicContext();
    ctx.Attach(logic);

    var tcs = new TaskCompletionSource<bool>();
    ctx.TrackTask(tcs.Task);
    logic.Task.IsCompleted.ShouldBeFalse();

    tcs.SetResult(true);
    await logic.Task;
    logic.Task.IsCompleted.ShouldBeTrue();
  }

  [Fact]
  public void TaskReturnsCompletedWhenNotAttached()
  {
    var ctx = new LogicContext();

    ctx.Task.IsCompleted.ShouldBeTrue();
  }

  [Fact]
  public void TrackTaskNoopsWhenNotAttached()
  {
    var ctx = new LogicContext();

    // Should not throw
    ctx.TrackTask(Task.CompletedTask);
  }

  [Fact]
  public void TaskSurvivesDetach()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);
    logic.Start<TestLogicBlockState>();

    var ctx = new LogicContext();
    ctx.Attach(logic);
    ctx.Detach();

    // Still forwards to logic block after detach
    ctx.Task.ShouldBe(logic.Task);
  }

  [Fact]
  public async Task TrackSurvivesDetach()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);
    logic.Start<TestLogicBlockState>();

    var ctx = new LogicContext();
    ctx.Attach(logic);
    ctx.Detach();

    var tcs = new TaskCompletionSource<bool>();
    ctx.TrackTask(tcs.Task);
    logic.Task.IsCompleted.ShouldBeFalse();

    tcs.SetResult(true);
    await logic.Task;
    logic.Task.IsCompleted.ShouldBeTrue();
  }

  [Fact]
  public void StatusReturnsStoppedWhenNotAttached()
  {
    var ctx = new LogicContext();

    ctx.Status.ShouldBe(LogicBlockStatus.Stopped);
  }

  [Fact]
  public void StatusForwardsToLogicBlockWhenAttached()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);
    logic.Start<TestLogicBlockState>();

    var ctx = new LogicContext();
    ctx.Attach(logic);

    ctx.Status.ShouldBe(LogicBlockStatus.Started);
  }
}
