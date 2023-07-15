namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class LogicBlockAsyncTest {
  [Fact]
  public void Initializes() {
    var block = new TestMachineAsync();
    var context = new TestMachineAsync.Context(block);
    block.Value.ShouldBe(block.GetInitialState(context));
  }

  [Fact]
  public async Task CallsEnterAndExitOnStatesInProperOrder() {
    var logic = new TestMachineAsync();
    var context = new TestMachineAsync.Context(logic);

    var outputs = new List<TestMachineAsync.Output>();

    void onOutput(object? block, TestMachineAsync.Output output) =>
      outputs.Add(output);

    logic.OnOutput += onOutput;

    logic.Value.ShouldBeOfType<TestMachineAsync.State.Deactivated>();
    var taskA = logic.Input(
      new TestMachineAsync.Input.Activate(SecondaryState.Blooped)
    );
    var taskB = logic.Input(
      new TestMachineAsync.Input.Deactivate()
    );
    taskA.ShouldBeSameAs(taskB);
    await logic.Input(
      new TestMachineAsync.Input.Activate(SecondaryState.Bopped)
    );
    // Repeating previous state should do nothing.
    await logic.Input(
      new TestMachineAsync.Input.Activate(SecondaryState.Bopped)
    );
    await logic.Input(
      new TestMachineAsync.Input.Activate(SecondaryState.Blooped)
    );
    await logic.Input(
      new TestMachineAsync.Input.Deactivate()
    );

    outputs.ShouldBe(new TestMachineAsync.Output[] {
      new TestMachineAsync.Output.DeactivatedCleanUp(),
      new TestMachineAsync.Output.Activated(),
      new TestMachineAsync.Output.Blooped(),
      new TestMachineAsync.Output.BloopedCleanUp(),
      new TestMachineAsync.Output.ActivatedCleanUp(),
      new TestMachineAsync.Output.Deactivated(),
      new TestMachineAsync.Output.DeactivatedCleanUp(),
      new TestMachineAsync.Output.Activated(),
      new TestMachineAsync.Output.Bopped(),
      new TestMachineAsync.Output.BoppedCleanUp(),
      new TestMachineAsync.Output.Blooped(),
      new TestMachineAsync.Output.BloopedCleanUp(),
      new TestMachineAsync.Output.ActivatedCleanUp(),
      new TestMachineAsync.Output.Deactivated(),
    });
  }

  [Fact]
  public async Task InvokesErrorEventFromUpdateHandler() {
    var block = new FakeLogicBlockAsync();

    var called = 0;

    void handler(object? _, Exception e) => called++;

    block.OnNextError += handler;

    block.Exceptions.ShouldBeEmpty();
    await block.Input(new FakeLogicBlockAsync.IInput.Custom(
      (context) => new FakeLogicBlockAsync.State.Custom(
          context,
          (context) => context.OnEnter<FakeLogicBlockAsync.State.Custom>(
            (previous) =>
              throw new InvalidOperationException("Error from OnEnter")
          )
        )
      )
    );
    block.Exceptions.ShouldNotBeEmpty();

    called.ShouldBe(1);

    block.OnNextError -= handler;
  }

  [Fact]
  public async Task DoesNothingOnUnhandledInput() {
    var block = new FakeLogicBlockAsync();
    var context = new FakeLogicBlockAsync.Context(block);
    await block.Input(new FakeLogicBlockAsync.IInput.InputUnknown());
    block.Value.ShouldBe(block.GetInitialState(context));
  }

  [Fact]
  public async Task InvokesErrorEvent() {
    var block = new FakeLogicBlockAsync();

    var called = 0;

    void handler(object? _, Exception e) => called++;

    block.OnNextError += handler;

    block.Exceptions.ShouldBeEmpty();
    await block.Input(new FakeLogicBlockAsync.IInput.InputError());
    block.Exceptions.ShouldNotBeEmpty();

    called.ShouldBe(1);

    block.OnNextError -= handler;
  }
}
