namespace Chickensoft.LogicBlocks.Tests;

using Collections;
using Fixtures;
using Shouldly;

using Input = Fixtures.TestLogicBlockState.Input;
using Output = Fixtures.TestLogicBlockState.Output;

public class BindingTest
{
  private static TestLogicBlock CreateLogicBlock()
  {
    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    bb.Set(new TestLogicBlockState.OutputtingState());
    return new TestLogicBlock(bb);
  }

  [Fact]
  public void OnStartFiresWhenStarted()
  {
    using var logic = CreateLogicBlock();
    using var binding = logic.Bind();
    var fired = false;

    binding.OnStart(() => fired = true);
    logic.Start<TestLogicBlockState>();

    fired.ShouldBeTrue();
  }

  [Fact]
  public void OnStopFiresWhenStopped()
  {
    using var logic = CreateLogicBlock();
    using var binding = logic.Bind();
    var fired = false;

    binding.OnStop(() => fired = true);
    logic.Start<TestLogicBlockState>();
    logic.Stop();

    fired.ShouldBeTrue();
  }

  [Fact]
  public void OnStateFiresForMatchingStateType()
  {
    using var logic = new LightSwitchLogic();
    using var binding = logic.Bind();
    LightSwitchState.PoweredOn? received = null;

    binding.OnState<LightSwitchState.PoweredOn>(state => received = state);

    logic.Start<LightSwitchState.PoweredOff>();
    logic.Input(new LightSwitchState.Input.TurnOn());

    received.ShouldNotBeNull();
  }

  [Fact]
  public void OnStateDoesNotFireForNonMatchingStateType()
  {
    using var logic = new LightSwitchLogic();
    using var binding = logic.Bind();
    var fired = false;

    binding.OnState<LightSwitchState.PoweredOn>(state => fired = true);

    // Start in PoweredOff — should not fire for PoweredOn
    logic.Start<LightSwitchState.PoweredOff>();

    fired.ShouldBeFalse();
  }

  [Fact]
  public void OnInputFiresWhenInputReceived()
  {
    using var logic = CreateLogicBlock();
    using var binding = logic.Bind();
    var fired = false;

    binding.OnInput((in Input.TestInput _) => fired = true);
    logic.Start<TestLogicBlockState>();
    logic.Input(new Input.TestInput());

    fired.ShouldBeTrue();
  }

  [Fact]
  public void OnOutputFiresWhenOutputEmitted()
  {
    using var logic = CreateLogicBlock();
    using var binding = logic.Bind();
    var fired = false;

    binding.OnOutput(
      (in Output.TestOutput _) => fired = true
    );

    logic.Start<TestLogicBlockState.OutputtingState>();
    logic.Input(new Input.TestInput());

    fired.ShouldBeTrue();
  }

  [Fact]
  public void OnLoadFiresWhenStartedFromData()
  {
    using var logic = CreateLogicBlock();
    using var binding = logic.Bind();
    var fired = false;

    binding.OnLoad(() => fired = true);

    var bb = new Blackboard();
    bb.Set(new TestLogicBlockState());
    var data = new LogicBlockData(
      typeof(TestLogicBlockState), bb, new History()
    );

    logic.Start(data);

    fired.ShouldBeTrue();
  }

  [Fact]
  public void DisposedBindingDoesNotReceiveCallbacks()
  {
    using var logic = CreateLogicBlock();
    var binding = logic.Bind();
    var fired = false;

    binding.OnStart(() => fired = true);
    binding.Dispose();

    logic.Start<TestLogicBlockState>();

    fired.ShouldBeFalse();
  }

  [Fact]
  public void FluentChainingReturnsBindingInstance()
  {
    using var logic = CreateLogicBlock();
    using var binding = logic.Bind();

    var result = binding
      .OnStart(() => { })
      .OnStop(() => { })
      .OnState<TestLogicBlockState>(_ => { })
      .OnInput((in Input.TestInput _) => { })
      .OnOutput((in Output.TestOutput _) => { })
      .OnLoad(() => { });

    result.ShouldBeSameAs(binding);
  }

  [Fact]
  public void OnStateFiresImmediatelyForInitialState()
  {
    using var logic = CreateLogicBlock();
    using var binding = logic.Bind();
    TestLogicBlockState? received = null;

    binding.OnState<TestLogicBlockState>(state => received = state);
    logic.Start<TestLogicBlockState>();

    // OnState should fire for the initial state too
    received.ShouldNotBeNull();
  }

  [Fact]
  public void MultipleBindingsReceiveCallbacks()
  {
    using var logic = CreateLogicBlock();
    using var binding1 = logic.Bind();
    using var binding2 = logic.Bind();
    var count = 0;

    binding1.OnStart(() => count++);
    binding2.OnStart(() => count++);

    logic.Start<TestLogicBlockState>();

    count.ShouldBe(2);
  }

  [Fact]
  public void OnOutputDoesNotFireForDifferentOutputType()
  {
    using var logic = new LightSwitchLogic();
    using var binding = logic.Bind();
    var fired = false;

    // Listen for TestOutput, but LightSwitch emits PlayToggleSound
    binding.OnOutput(
      (in Output.TestOutput _) => fired = true
    );

    logic.Start<LightSwitchState.PoweredOff>();

    fired.ShouldBeFalse();
  }
}
