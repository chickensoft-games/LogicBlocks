namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class LogicBlockAsyncTest {
  [Fact]
  public void Initializes() {
    var block = new TestMachineAsync();
    var context = new TestMachineAsync.DefaultContext(block);
    block.Value.ShouldBe(block.GetInitialState(context));
  }

  [Fact]
  public async Task CallsEnterAndExitOnStatesInProperOrder() {
    var logic = new TestMachineAsync();
    var context = new TestMachineAsync.DefaultContext(logic);

    var outputs = new List<object>();

    void onOutput(object output) => outputs.Add(output);

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

    outputs.ShouldBe(new object[] {
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
  public async Task CallsEnterAndExitOnStatesInProperOrderForReusedStates() {
    var logic = new TestMachineReusableAsync();
    var outputs = new List<object>();

    void onOutput(object output) => outputs.Add(output);

    logic.OnOutput += onOutput;

    logic.Value.ShouldBeOfType<TestMachineReusableAsync.State.Deactivated>();
    var taskA = logic.Input(
      new TestMachineReusableAsync.Input.Activate(SecondaryState.Blooped)
    );
    var taskB = logic.Input(
      new TestMachineReusableAsync.Input.Deactivate()
    );
    taskA.ShouldBeSameAs(taskB);
    await logic.Input(
      new TestMachineReusableAsync.Input.Activate(SecondaryState.Bopped)
    );
    // Repeating previous state should do nothing.
    await logic.Input(
      new TestMachineReusableAsync.Input.Activate(SecondaryState.Bopped)
    );
    await logic.Input(
      new TestMachineReusableAsync.Input.Activate(SecondaryState.Blooped)
    );
    await logic.Input(
      new TestMachineReusableAsync.Input.Deactivate()
    );

    outputs.ShouldBe(new object[] {
      new TestMachineReusableAsync.Output.DeactivatedCleanUp(),
      new TestMachineReusableAsync.Output.Activated(),
      new TestMachineReusableAsync.Output.Blooped(),
      new TestMachineReusableAsync.Output.BloopedCleanUp(),
      new TestMachineReusableAsync.Output.ActivatedCleanUp(),
      new TestMachineReusableAsync.Output.Deactivated(),
      new TestMachineReusableAsync.Output.DeactivatedCleanUp(),
      new TestMachineReusableAsync.Output.Activated(),
      new TestMachineReusableAsync.Output.Bopped(),
      new TestMachineReusableAsync.Output.BoppedCleanUp(),
      new TestMachineReusableAsync.Output.Blooped(),
      new TestMachineReusableAsync.Output.BloopedCleanUp(),
      new TestMachineReusableAsync.Output.ActivatedCleanUp(),
      new TestMachineReusableAsync.Output.Deactivated(),
    });
  }

  [Fact]
  public async Task InvokesErrorEventFromUpdateHandler() {
    var block = new FakeLogicBlockAsync();

    var called = 0;

    void handler(Exception e) => called++;

    block.OnError += handler;

    await block.Input(new FakeLogicBlockAsync.Input.Custom(
      (context) => new FakeLogicBlockAsync.State.OnEnterState(
          (previous) =>
            throw new InvalidOperationException("Error from OnEnter")
        )
      )
    );

    called.ShouldBe(1);

    block.OnError -= handler;
  }

  [Fact]
  public async Task InvokesErrorEventFromUpdateHandlerManually() {
    var block = new FakeLogicBlockAsync() {
      InitialState = () => new FakeLogicBlockAsync.State.OnEnterState(
        (previous) =>
          throw new InvalidOperationException("Error from OnEnter")
      )
    };

    await Should.ThrowAsync<InvalidOperationException>(
      async () => await block.Value.Enter()
    );
  }

  [Fact]
  public async Task DoesNothingOnUnhandledInput() {
    var block = new FakeLogicBlockAsync();
    var context = new FakeLogicBlockAsync.DefaultContext(block);
    await block.Input(new FakeLogicBlockAsync.Input.InputUnknown());
    block.Value.ShouldBe(block.GetInitialState(context));
  }

  [Fact]
  public async Task InvokesErrorEvent() {
    var block = new FakeLogicBlockAsync();

    var called = 0;

    void handler(Exception e) => called++;

    block.OnError += handler;

    await block.Input(new FakeLogicBlockAsync.Input.InputError());

    called.ShouldBe(1);

    block.OnError -= handler;
  }

  [Fact]
  public async Task ThrowingFromHandleErrorStopsExecution() {
    var block = new FakeLogicBlockAsync((e) => throw e);

    var e = await Should.ThrowAsync<Exception>(
      async () => await block.Input(new FakeLogicBlockAsync.Input.InputError())
    );

    e.InnerException.ShouldBeOfType<InvalidOperationException>();
  }

  [Fact]
  public async Task StateCanAddInputUsingContext() {
    var logic = new FakeLogicBlockAsync();
    var input = new FakeLogicBlockAsync.Input.InputOne(5, 6);

    await logic.Input(new FakeLogicBlockAsync.Input.SelfInput(input));

    logic.Value.ShouldBeOfType<FakeLogicBlockAsync.State.StateA>();
  }

  [Fact]
  public async Task StartsManuallyAndIgnoresStartWhenProcessing() {
    var enterCalled = false;
    var block = new FakeLogicBlockAsync() {
      InitialState = () =>
        new FakeLogicBlockAsync.State.OnEnterState(
          (previous) => {
            enterCalled = true;
            return Task.CompletedTask;
          }
        )
    };

    // LogicBlocks shouldn't call entrance handlers for the initial state.
    enterCalled.ShouldBeFalse();

    await block.Value.Enter();

    enterCalled.ShouldBeTrue();
  }

  [Fact]
  public async Task StartEntersState() {
    var enterCalled = false;
    var block = new FakeLogicBlockAsync() {
      InitialState = () =>
        new FakeLogicBlockAsync.State.OnEnterState(
          (previous) => {
            enterCalled = true;
            return Task.CompletedTask;
          })
    };

    enterCalled.ShouldBeFalse();

    await block.Start();

    enterCalled.ShouldBeTrue();
  }

  [Fact]
  public async Task StopExitsState() {
    var exitCalled = false;
    var block = new FakeLogicBlockAsync() {
      InitialState = () =>
        new FakeLogicBlockAsync.State.OnExitState(
          (previous) => {
            exitCalled = true;
            return Task.CompletedTask;
          })
    };

    exitCalled.ShouldBeFalse();

    await block.Stop();

    exitCalled.ShouldBeTrue();
  }
}
