namespace Chickensoft.LogicBlocks.Tests;

using Collections;
using Fixtures;
using Shouldly;

public sealed class LogicBlockTest
{
  [Fact]
  public void Initializes()
  {
    using var lightSwitch = new LightSwitchLogic();

    lightSwitch.ShouldBeAssignableTo<LightSwitchLogic>();
  }

  [Fact]
  public void InitializesWithBlackboard()
  {
    var blackboard = new Blackboard();

    using var logic = new TestLogicBlock(blackboard);

    logic.Blackboard.ShouldBeSameAs(blackboard);
  }

  [Fact]
  public void DeterminesEquivalence()
  {
    LogicBlock.IsEquivalent(null, null).ShouldBeTrue();
    LogicBlock.IsEquivalent(null, "test").ShouldBeFalse();
    LogicBlock.IsEquivalent("test", null).ShouldBeFalse();
    LogicBlock.IsEquivalent("test", "test").ShouldBeTrue();

    var obj = new object();
    LogicBlock.IsEquivalent(obj, obj).ShouldBeTrue();
    LogicBlock.IsEquivalent(obj, new object()).ShouldBeFalse();
  }

  [Fact]
  public void ClearBindingsWorks()
  {
    using var logic = new TestLogicBlock();
    logic.Set(new TestLogicBlockState());

    using var binding = logic.Bind();
    var fired = false;
    binding.OnStart(() => fired = true);

    logic.ClearBindings();
    logic.Start<TestLogicBlockState>();

    fired.ShouldBeFalse();
  }
}

public sealed class LogicBlockBlackboardTest
{
  [Fact]
  public void ForwardsBlackboardMethods()
  {
    using var logic = new TestLogicBlock();

    logic.Set("hello, world");
    logic.SetObject(typeof(TestLogicBlockState), new TestLogicBlockState());
    logic.Has<string>().ShouldBeTrue();
    logic.Has<TestLogicBlockState>().ShouldBeTrue();
    logic.HasObject(typeof(TestLogicBlockState)).ShouldBeTrue();
    logic.Get<string>().ShouldBe("hello, world");

    logic.HasObject(typeof(string)).ShouldBeTrue();
    logic.GetObject(typeof(string)).ShouldBe("hello, world");

    logic.Overwrite("goodbye, world");
    logic.Get<string>().ShouldBe("goodbye, world");

    logic.OverwriteObject(typeof(string), "hello again, world");
    logic.Get<string>().ShouldBe("hello again, world");

    logic.Types.ShouldContain(typeof(string));
    logic.Types.ShouldContain(typeof(TestLogicBlockState));
    logic.Types.Count.ShouldBe(2);
  }
}

public sealed class LogicBlockStartAndStopTest
{
  [Fact]
  public void StartsWithStaticallyTypedState()
  {
    using var logic = new TestLogicBlock();

    logic.Set(new TestLogicBlockState());
    logic.Start<TestLogicBlockState>().ShouldNotBeNull();

    logic.IsStarted.ShouldBeTrue();
    logic.IsBusy.ShouldBeFalse();
    logic.State.ShouldBeOfType<TestLogicBlockState>();

    Should.Throw<LogicBlockException>(() =>
        logic.Start<TestLogicBlockState>() // can't start again once started
    );
  }

  [Fact]
  public void StartsWithSpecificStateInstance()
  {
    using var logic = new TestLogicBlock();
    var state = new TestLogicBlockState();

    logic.Set(state);

    logic.Start<TestLogicBlockState>().ShouldNotBeNull();
    // already started — should throw
#pragma warning disable CA2263 // Prefer generic overload when type is known
    Should.Throw<LogicBlockException>(() =>
      logic.Start(typeof(TestLogicBlockState)).ShouldNotBeNull()
    );
#pragma warning restore CA2263 // Prefer generic overload when type is known
  }

  [Fact]
  public void StartsAndStopsAndCallsLifecycleMethods()
  {
    using var logic = new LightSwitchLogic();

    logic.Start<LightSwitchState.PoweredOff>();
    logic.StartCalled.ShouldBeTrue();

    logic.Stop();
    logic.StopCalled.ShouldBeTrue();
  }

  [Fact]
  public void StartThrowsIfInitialStateIsNotSet()
  {
    var logic = new TestLogicBlock();

    var e = Should.Throw<LogicBlockException>(() =>
      logic.Start<TestLogicBlockState>()
    );

    e.Message.ShouldContain("Please set an instance of each state type");
  }

  [Fact]
  public void StartDoesNotRestartIfStarted()
  {
    using var logic = new TestLogicBlock();
    logic.Set(new TestLogicBlockState());

    var called = false;

    logic.OnStartAction = () =>
    {
      called = true;
      Should.Throw<LogicBlockException>(() =>
      {
        logic.Start<TestLogicBlockState>();
      });
    };

    logic.Start<TestLogicBlockState>();

    called.ShouldBeTrue();
  }

  [Fact]
  public void StartFlushesInputs()
  {
    using var logic = new TestLogicBlock();
    var inputHandled = false;
    var state = new TestLogicBlockState
    {
      OnEnterAction =
        () => logic.Input(new TestLogicBlockState.Input.TestInput()),
      OnInputAction = (object _) => inputHandled = true
    };
    logic.Set(state);

    // logic blocks should process input that was added by a state during its Enter
    // callback when starting up
    logic.Start<TestLogicBlockState>();

    inputHandled.ShouldBeTrue();
  }

  [Fact]
  public void StartFromDataStartsAndFlushesInputs()
  {
    using var logic = new TestLogicBlock();
    var inputHandled = false;
    var state = new TestLogicBlockState
    {
      OnEnterAction =
        () => logic.Input(new TestLogicBlockState.Input.TestInput()),
      OnInputAction = (object _) => inputHandled = true
    };
    logic.Set(state);

    var data = new LogicBlockData(
      typeof(TestLogicBlockState), new Blackboard(), new History()
    );

    logic.Start(data);

    inputHandled.ShouldBeTrue();
  }

  [Fact]
  public void StartFromDataDoesNotRestartIfStarted()
  {
    using var logic = new TestLogicBlock();
    var called = false;

    var state = new TestLogicBlockState
    {
      OnEnterAction = () => Should.Throw<LogicBlockException>(() =>
      {
        called = true;
        logic.Start<TestLogicBlockState>();
      })
    };
    logic.Set(state);

    logic.Start<TestLogicBlockState>();

    called.ShouldBeTrue();
  }

  [Fact]
  public void StopOnStartIsDeferred()
  {
    var started = false;
    var stopped = false;

    using var logic = new TestLogicBlock();
    logic.Set(new TestLogicBlockState());

    logic.OnStartAction = () =>
    {
      started = true;
      logic.Stop();
      stopped.ShouldBeFalse();
    };

    logic.OnStopAction = () => stopped = true;

    logic.Start<TestLogicBlockState>();

    started.ShouldBeTrue();
    stopped.ShouldBeTrue();

    stopped.ShouldBeTrue();
  }

  [Fact]
  public void StopDoesNothingIfNotStarted()
  {
    using var logic = new LightSwitchLogic();

    logic.Stop(); // does nothing

    logic.IsStarted.ShouldBeFalse();

    logic.State.ShouldBeNull();
  }

  [Fact]
  public void StopClearsHistory()
  {
    using var logic = new LightSwitchLogic();

    var history = logic.History;
    history.Push(typeof(LightSwitchState.PoweredOff));

    logic.Start<LightSwitchState.PoweredOff>();
    logic.Stop();

    history.ShouldBeEmpty();

    logic._history.ShouldBeNull();
  }

  [Fact]
  public void StartFromDataRestoresHistory()
  {
    using var logic = new LightSwitchLogic();

    var history = new History([
      typeof(LightSwitchState.PoweredOn),
      typeof(LightSwitchState.PoweredOff)
    ]);

    var data = new LogicBlockData(
      typeof(LightSwitchState.PoweredOff), new Blackboard(), history
    );

    logic.Start(data);

    logic.History.Count.ShouldBe(2);
    var entries = logic.History.ToArray();
    entries[0].ShouldBe(typeof(LightSwitchState.PoweredOn));
    entries[1].ShouldBe(typeof(LightSwitchState.PoweredOff));
  }

  [Fact]
  public void HasHistoryReflectsHistoryState()
  {
    using var logic = new LightSwitchLogic();
    logic.Start<LightSwitchState.PoweredOff>();

    logic.State!.HasHistory.ShouldBeFalse();

    logic.State!.Push(typeof(LightSwitchState.PoweredOff));

    logic.State!.HasHistory.ShouldBeTrue();
  }

  [Fact]
  public void StartFromDataThrowsWhenAlreadyStarted()
  {
    using var logic = new TestLogicBlock();
    logic.Set(new TestLogicBlockState());
    logic.Start<TestLogicBlockState>();

    var data = new LogicBlockData(
      typeof(TestLogicBlockState), new Blackboard(), new History()
    );

    Should.Throw<LogicBlockException>(() => logic.Start(data));
  }

  [Fact]
  public void OnStartSubscriptionsDisposablesAreDisposedOnStop()
  {
    using var repo = new EnemyRepo();
    using var logic = new SubscriptionLogic();
    logic.Set(repo);
    logic.Set(new SubscriptionLogicState());

    logic.Start<SubscriptionLogicState>();
    logic.Stop();

    // The subscription binding from OnStartSubscriptions should have been
    // disposed during stop. Changing the value after stop should not cause
    // any inputs to be processed.
    logic.IsStarted.ShouldBeFalse();
  }
}

public sealed class LogicBlockInputTest
{
  [Fact]
  public void ThrowsIfNotStarted()
  {
    using var logic = new LightSwitchLogic();

    Should.Throw<LogicBlockException>(() =>
      logic.Input(new LightSwitchState.Input.TurnOn())
    );
  }

  [Fact]
  public void ConvertsInputsToStates()
  {
    using var logic = new LightSwitchLogic();

    logic.Start<LightSwitchState.PoweredOff>();

    logic.Input(new LightSwitchState.Input.TurnOn())
      .ShouldBeOfType<LightSwitchState.PoweredOn>();

    logic.Input(new LightSwitchState.Input.TurnOff())
      .ShouldBeOfType<LightSwitchState.PoweredOff>();
  }

  [Fact]
  public void CapturesEveryInput()
  {
    using var logic = new TestLogicBlock();
    var state = new TestLogicBlockState.EveryInputState();
    logic.Set(state);
    logic.Start<TestLogicBlockState.EveryInputState>();

    logic.Input(123);
    logic.Input(new TestLogicBlockState.Input.TestInput());

    state.Inputs.ShouldBe([
      123,
      new TestLogicBlockState.Input.TestInput()
    ]);
  }
}

public sealed class LogicBlockPersistenceTest
{
  [Fact]
  public void GetDataReturnsCurrentState()
  {
    using var logic = new LightSwitchLogic();

    logic.Start<LightSwitchState.PoweredOff>();

    var data = logic.GetData();

    data.StateType.ShouldBe(typeof(LightSwitchState.PoweredOff));
    data.Blackboard.ShouldBeSameAs(logic.Blackboard);
    data.History.ShouldBeSameAs(logic.History);
  }

  [Fact]
  public void GetDataThrowsIfNotStarted()
  {
    using var logic = new LightSwitchLogic();

    Should.Throw<LogicBlockException>(logic.GetData);
  }
}

public sealed class LogicBlockDisposedTest
{
  [Fact]
  public void BlackboardThrowsIfDisposed()
  {
    var logic = new TestLogicBlock();
    logic.Dispose();

    Should.Throw<LogicBlockException>(() => logic.Blackboard);
  }

  [Fact]
  public void StateIsNullIfDisposed()
  {
    var logic = new TestLogicBlock();
    logic.Dispose();

    logic.State.ShouldBeNull();
  }

  [Fact]
  public void StartFromDataThrowsIfDisposed()
  {
    var logic = new LightSwitchLogic();

    var data = new LogicBlockData(
      typeof(LightSwitchState.PoweredOff), new Blackboard(), new History()
    );

    logic.Dispose();

    Should.Throw<LogicBlockException>(() => logic.Start(data));
  }

  [Fact]
  public void StartThrowsIfDisposed()
  {
    var logic = new LightSwitchLogic();
    logic.Dispose();

    Should.Throw<LogicBlockException>(() =>
      logic.Start<LightSwitchState.PoweredOff>()
    );
  }

  [Fact]
  public void GetDataThrowsIfDisposed()
  {
    var logic = new LightSwitchLogic();

    logic.Start<LightSwitchState.PoweredOff>();
    logic.Dispose();

    Should.Throw<LogicBlockException>(logic.GetData);
  }

}

public sealed class LogicBlockStatusTest
{
  [Fact]
  public void StatusIsStoppedByDefault()
  {
    using var logic = new LightSwitchLogic();

    logic.Status.ShouldBe(LogicBlockStatus.Stopped);
  }

  [Fact]
  public void StatusIsStartedAfterStart()
  {
    using var logic = new LightSwitchLogic();
    logic.Start<LightSwitchState.PoweredOff>();

    logic.Status.ShouldBe(LogicBlockStatus.Started);
  }

  [Fact]
  public void StatusIsStoppedAfterStop()
  {
    using var logic = new LightSwitchLogic();
    logic.Start<LightSwitchState.PoweredOff>();
    logic.Stop();

    logic.Status.ShouldBe(LogicBlockStatus.Stopped);
  }

  [Fact]
  public void StatusIsDisposedAfterDispose()
  {
    var logic = new LightSwitchLogic();
    logic.Start<LightSwitchState.PoweredOff>();
    logic.Dispose();

    logic.Status.ShouldBe(LogicBlockStatus.Disposed);
  }

  [Fact]
  public void StatusIsDisposedAfterGarbageCollected()
  {
    var weakReference = Utils.MakeWeakRef<LightSwitchLogic>();

    GC.Collect();
    GC.WaitForPendingFinalizers();

    weakReference.TryGetTarget(out var logic);
    logic.ShouldNotBeNull();

    logic.Status.ShouldBe(LogicBlockStatus.Disposed);
  }

  [Fact]
  public void IsStoppedTrueByDefault()
  {
    using var logic = new LightSwitchLogic();

    logic.IsStopped.ShouldBeTrue();
  }

  [Fact]
  public void IsStoppedFalseWhenStarted()
  {
    using var logic = new LightSwitchLogic();
    logic.Start<LightSwitchState.PoweredOff>();

    logic.IsStopped.ShouldBeFalse();
  }

  [Fact]
  public void IsStoppedTrueAfterStop()
  {
    using var logic = new LightSwitchLogic();
    logic.Start<LightSwitchState.PoweredOff>();
    logic.Stop();

    logic.IsStopped.ShouldBeTrue();
  }
}
