namespace Chickensoft.LogicBlocks.Tests;

using Fixtures;
using Shouldly;

using Input = Fixtures.TestLogicBlockState.Input;
using Output = Fixtures.TestLogicBlockState.Output;

public class FakeBindingTest
{
  [Fact]
  public void InputFiresOnInputCallback()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnInput((in Input.TestInput _) => fired = true);
    binding.Input(new Input.TestInput());

    fired.ShouldBeTrue();
  }

  [Fact]
  public void StateFiresOnStateCallback()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    TestLogicBlockState? received = null;

    binding.OnState<TestLogicBlockState>(state => received = state);

    var state = new TestLogicBlockState();
    binding.SetState(state);

    received.ShouldBeSameAs(state);
  }

  [Fact]
  public void StateFiresOnExitStateCallback()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    TestLogicBlockState? received = null;

    binding.OnExitState<TestLogicBlockState>(state => received = state);

    var state = new TestLogicBlockState();
    binding.SetExitState(state);

    received.ShouldBeSameAs(state);
  }

  [Fact]
  public void StateDoesNotFireForNonMatchingType()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnState<TestLogicBlockState.OutputtingState>(_ => fired = true);
    binding.SetState(new TestLogicBlockState());

    fired.ShouldBeFalse();
  }

  [Fact]
  public void StateExitDoesNotFireForNonMatchingType()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnExitState<TestLogicBlockState.OutputtingState>(_ => fired = true);
    binding.SetExitState(new TestLogicBlockState());

    fired.ShouldBeFalse();
  }

  [Fact]
  public void StateDoesNotFireForDerivedType()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnState<TestLogicBlockState>(_ => fired = true);
    binding.SetState(new TestLogicBlockState.SubState(), new TestLogicBlockState());

    fired.ShouldBeFalse();
  }

  [Fact]
  public void StateExitDoesNotFireForDerivedType()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnExitState<TestLogicBlockState>(_ => fired = true);
    binding.SetExitState(new TestLogicBlockState(), new TestLogicBlockState.SubState());

    fired.ShouldBeFalse();
  }

  [Fact]
  public void OutputFiresOnOutputCallback()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnOutput((in Output.TestOutput _) => fired = true);
    binding.Output(new Output.TestOutput());

    fired.ShouldBeTrue();
  }

  [Fact]
  public void StartFiresOnStartCallback()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnStart(() => fired = true);
    binding.Start();

    fired.ShouldBeTrue();
  }

  [Fact]
  public void StopFiresOnStopCallback()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnStop(() => fired = true);
    binding.Stop();

    fired.ShouldBeTrue();
  }

  [Fact]
  public void LoadFiresOnLoadCallback()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var fired = false;

    binding.OnLoad(() => fired = true);
    binding.Load();

    fired.ShouldBeTrue();
  }

  [Fact]
  public void FluentChainingWorksWithFakeBinding()
  {
    using var binding = LogicBlock.CreateFakeBinding();

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
  public void MultipleCallbacksFireInOrder()
  {
    using var binding = LogicBlock.CreateFakeBinding();
    var log = new List<int>();

    binding.OnStart(() => log.Add(1));
    binding.OnStart(() => log.Add(2));
    binding.OnStart(() => log.Add(3));

    binding.Start();

    log.ShouldBe([1, 2, 3]);
  }
}
