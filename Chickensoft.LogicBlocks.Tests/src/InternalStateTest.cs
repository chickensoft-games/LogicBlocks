namespace Chickensoft.LogicBlocks.Tests;

using Collections;
using Fixtures;
using Shouldly;

public class InternalStateTest
{
  [Fact]
  public void TestReturnsStateTester()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    tester.ShouldNotBeNull();
    tester.ShouldBeOfType<StateTester>();
  }

  [Fact]
  public void TestThrowsWhenAttachedToLogicBlock()
  {
    var bb = new Blackboard();
    var state = new TestLogicBlockState();
    bb.Set(state);
    using var logic = new TestLogicBlock(bb);
    logic.Start<TestLogicBlockState>();

    Should.Throw<LogicBlockException>(state.Test);
  }

  [Fact]
  public void TestReusesExistingTester()
  {
    var state = new TestLogicBlockState();
    var tester1 = state.Test();
    tester1.Set("data");

    var tester2 = state.Test();

    tester2.ShouldBeSameAs(tester1);
    // tester.Reset() should have been called
    tester2.Inputs.Count.ShouldBe(0);
  }

  [Fact]
  public void OnEnterCallbackFiresWhenPreviousDoesNotMatch()
  {
    var state = new LightSwitchState.PoweredOn();
    var tester = state.Test();

    // PoweredOn has an OnEnter
    // Enter from a different type should invoke
    state.Enter(null);

    // On enter should have produced an output
    tester.Outputs.Count.ShouldBe(1);
  }

  [Fact]
  public void OnEnterCallbackSkipsWhenPreviousMatchesPredicate()
  {
    var state = new LightSwitchState.PoweredOn();
    var tester = state.Test();

    // Enter from the same type
    state.Enter(new LightSwitchState.PoweredOn());

    tester.Outputs.Count.ShouldBe(0);
  }

  [Fact]
  public void OnEnterCallbackSkipsWhenNextIsNull()
  {
    var internalState = new InternalState();
    var fired = false;

    internalState.AddOnEnterCallback<TestLogicBlockState>(() => fired = true);
    internalState.CallOnEnterCallbacks(null, null);

    fired.ShouldBeFalse();
  }

  [Fact]
  public void OnExitCallbackFiresWhenNextDoesNotMatch()
  {
    var fired = false;
    var state = new TestLogicBlockState();
    state.InternalState.AddOnExitCallback<TestLogicBlockState>(
      () => fired = true
    );
    var tester = state.Test();

    // Exit to a different type
    state.Exit(new LightSwitchState.PoweredOn());

    fired.ShouldBeTrue();
  }

  [Fact]
  public void OnExitCallbackSkipsWhenNextMatchesPredicate()
  {
    var fired = false;
    var state = new TestLogicBlockState();
    state.InternalState.AddOnExitCallback<TestLogicBlockState>(
      () => fired = true
    );
    var tester = state.Test();

    // Exit to the same type
    state.Exit(new TestLogicBlockState());

    fired.ShouldBeFalse();
  }

  [Fact]
  public void OnExitCallbackSkipsWhenPreviousIsNull()
  {
    var internalState = new InternalState();
    var fired = false;

    internalState.AddOnExitCallback<TestLogicBlockState>(() => fired = true);
    internalState.CallOnExitCallbacks(null, null);

    fired.ShouldBeFalse();
  }

  [Fact]
  public void EqualsAlwaysReturnsTrue()
  {
    var a = new InternalState();
    var b = new InternalState();

    a.Equals(b).ShouldBeTrue();
    a.Equals("whatever").ShouldBeTrue();
    a.Equals(null).ShouldBeTrue();
  }

  [Fact]
  public void GetHashCodeReturnsValue()
  {
    var a = new InternalState();
    a.GetHashCode().ShouldBeOfType<int>();
  }

  [Fact]
  public async Task ForwardingDelegatesToContext()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();

    // Input/Output forwarding
    state.InternalState.Input(new TestLogicBlockState.Input.TestInput());
    tester.Inputs.Count.ShouldBe(1);

    state.InternalState.Output(new TestLogicBlockState.Output.TestOutput());
    tester.Outputs.Count.ShouldBe(1);

    // Blackboard forwarding
    tester.Set("hello");
    state.InternalState.Get<string>().ShouldBe("hello");
    state.InternalState.Has<string>().ShouldBeTrue();

    // History forwarding
    var pushed = typeof(TestLogicBlockState);
    state.InternalState.Push(pushed);
    state.InternalState.Peek().ShouldBe(pushed);
    state.InternalState.Pop().ShouldBe(pushed);

    state.InternalState.Push(pushed);
    state.InternalState.ClearHistory();
    tester.History.Count.ShouldBe(0);

    // Task tracking forwarding
    state.InternalState.TrackTask(Task.CompletedTask);
    await state.InternalState.Task;
    state.InternalState.Task.IsCompleted.ShouldBeTrue();
  }

  [Fact]
  public void AttachAndDetachForwardToContext()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    using var logic = new TestLogicBlock(bb);

    var internalState = new InternalState();
    internalState.IsAttached.ShouldBeFalse();

    internalState.Attach(logic);
    internalState.IsAttached.ShouldBeTrue();

    internalState.Detach();
    internalState.IsAttached.ShouldBeFalse();
  }

  [Fact]
  public void StatusForwardsToContext()
  {
    var state = new TestLogicBlockState();

    _ = state.Test();

    // StateTester always reports Started
    state.InternalState.Status.ShouldBe(LogicBlockStatus.Started);
  }
}
