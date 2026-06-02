namespace Chickensoft.LogicBlocks.Tests;

using Fixtures;
using Shouldly;

public class LogicBlockStateTest
{
  [Fact]
  public void EnterThrowsWhenNotAttached()
  {
    var state = new TestLogicBlockState();

    Should.Throw<LogicBlockException>(() => state.Enter(null));
  }

  [Fact]
  public void ExitThrowsWhenNotAttached()
  {
    var state = new TestLogicBlockState();

    Should.Throw<LogicBlockException>(() => state.Exit(null));
  }

  [Fact]
  public void IsAttachedFalseByDefault()
  {
    var state = new TestLogicBlockState();

    state.IsAttached.ShouldBeFalse();
  }

  [Fact]
  public void IsAttachedTrueAfterTest()
  {
    var state = new TestLogicBlockState();
    state.Test();

    state.IsAttached.ShouldBeTrue();
  }

  [Fact]
  public void ToReturnsType()
  {
    var state = new LightSwitchState.PoweredOff();

    _ = state.Test();

    var result = state.On(new LightSwitchState.Input.TurnOn());

    result.ShouldBe(typeof(LightSwitchState.PoweredOn));
  }

  [Fact]
  public void ToSelfReturnsOwnType()
  {
    var state = new TestLogicBlockState();

    _ = state.Test();

    var result = state.On(new TestLogicBlockState.Input.TestInput());

    result.ShouldBe(typeof(TestLogicBlockState));
  }

  [Fact]
  public void OutputFromState()
  {
    var state = new TestLogicBlockState.OutputtingState();
    var tester = state.Test();

    state.On(new TestLogicBlockState.Input.TestInput());

    tester.Outputs.Count.ShouldBe(1);
  }

  [Fact]
  public void InputFromState()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    state.Input(new TestLogicBlockState.Input.TestInput());

    tester.Inputs.Count.ShouldBe(1);
  }

  [Fact]
  public void GetFromState()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();
    tester.Set("hello");

    state.Get<string>().ShouldBe("hello");
  }

  [Fact]
  public void HasFromState()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    state.Has<string>().ShouldBeFalse();
    tester.Set("hello");
    state.Has<string>().ShouldBeTrue();
  }

  [Fact]
  public void HistoryFromState()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();
    var pushed = typeof(TestLogicBlockState);

    state.Push(pushed);
    state.Peek().ShouldBe(pushed);
    state.Pop().ShouldBe(pushed);

    state.Push(pushed);
    state.ClearHistory();
    tester.History.Count.ShouldBe(0);
  }

  [Fact]
  public void TestMethodReturnsStateTester()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.ShouldBeOfType<StateTester>();
  }

  [Fact]
  public void HasHistoryReturnsFalseWhenEmpty()
  {
    var state = new TestLogicBlockState();
    _ = state.Test();

    state.HasHistory.ShouldBeFalse();
  }

  [Fact]
  public void HasHistoryReturnsTrueAfterPush()
  {
    var state = new TestLogicBlockState();
    _ = state.Test();

    state.Push(typeof(TestLogicBlockState));

    state.HasHistory.ShouldBeTrue();
  }

  [Fact]
  public void ParameterlessPushPushesCurrentState()
  {
    var state = new TestLogicBlockState();
    state.Test();

    state.Push();

    state.HasHistory.ShouldBeTrue();
    state.Peek().ShouldBe(typeof(TestLogicBlockState));
  }

  [Fact]
  public void StatusForwardsToInternalState()
  {
    var state = new TestLogicBlockState();
    state.Test();

    // StateTester always reports Started
    state.Status.ShouldBe(LogicBlockStatus.Started);
  }

  [Fact]
  public void TaskForwardsToContext()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    state.Task.ShouldBe(tester.Task);
  }

  [Fact]
  public void TrackTaskForwardsToContext()
  {
    var state = new TestLogicBlockState();

    _ = state.Test();

    state.TrackTask(Task.CompletedTask);
    state.Task.IsCompleted.ShouldBeTrue();
  }
}
