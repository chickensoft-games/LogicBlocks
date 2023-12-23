namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class LogicBlockTest {
  [Fact]
  public void Initializes() {
    var block = new FakeLogicBlock();
    block.Value.ShouldBe(block.GetInitialState());
  }

  [Fact]
  public void GetsAndSetsBlackboardData() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.FakeContext();
    block.PublicSet("data");
    block.Get<string>().ShouldBe("data");

    // Can't change values once set.
    Should.Throw<ArgumentException>(() => block.PublicSet("other"));
    Should.Throw<KeyNotFoundException>(() => block.Get<int>());
    block.Input(new FakeLogicBlock.Input.GetString());
    block.Value.ShouldBe(new FakeLogicBlock.State.StateC("data"));
  }

  [Fact]
  public void InvokesInputEvent() {
    var block = new FakeLogicBlock();

    var called = 0;
    var input = new FakeLogicBlock.Input.InputOne(2, 3);

    void handler(object input) {
      input.ShouldBe(input);
      called++;
    }

    block.OnInput += handler;

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(1);

    block.OnInput -= handler;

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(1);
  }

  [Fact]
  public void InvokesOutputEvent() {
    var block = new FakeLogicBlock();

    var called = 0;
    var output = new FakeLogicBlock.Output.OutputOne(2);

    void handler(object output) {
      output.ShouldBe(output);
      called++;
    }

    block.OnOutput += handler;

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(1);

    block.OnOutput -= handler;

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(1);
  }

  [Fact]
  public void InvokesStateEvent() {
    var block = new FakeLogicBlock();

    var called = 0;
    var state = new FakeLogicBlock.State.StateA(2, 3);

    void handler(FakeLogicBlock.State state) {
      state.ShouldBe(state);
      called++;
    }

    block.OnState += handler;

    called.ShouldBe(0);

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(1);

    block.OnState -= handler;
  }

  [Fact]
  public void InvokesErrorEvent() {
    var block = new FakeLogicBlock();

    var called = 0;

    void handler(Exception e) => called++;

    block.OnError += handler;

    block.Input(new FakeLogicBlock.Input.InputError());

    called.ShouldBe(1);

    block.OnError -= handler;
  }

  [Fact]
  public void ThrowingFromHandleErrorStopsExecution() {
    var block = new FakeLogicBlock((e) => throw e);

    Should.Throw<InvalidOperationException>(
      () => block.Input(new FakeLogicBlock.Input.InputError())
    );
  }

  [Fact]
  public void StateCanCallAddErrorFromContext() {
    Exception? error = null;

    var exception = new InvalidOperationException();

    var block = new FakeLogicBlock((e) => error = e);

    block.Input(new FakeLogicBlock.Input.Custom((context) => {
      context.AddError(exception);
      return block.GetInitialState();
    }));

    error.ShouldBe(exception);
  }

  [Fact]
  public void DoesNothingOnUnhandledInput() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.DefaultContext(block);
    block.Input(new FakeLogicBlock.Input.InputUnknown());
    block.Value.ShouldBe(block.GetInitialState());
  }

  [Fact]
  public void CallsEnterAndExitOnStatesInProperOrder() {
    var logic = new TestMachine();
    var context = new TestMachine.DefaultContext(logic);

    var outputs = new List<object>();

    void onOutput(object output) => outputs.Add(output);

    logic.OnOutput += onOutput;

    logic.Value.ShouldBeOfType<TestMachine.State.Deactivated>();
    logic.Input(
      new TestMachine.Input.Activate(SecondaryState.Blooped)
    );
    logic.Input(
      new TestMachine.Input.Deactivate()
    );
    logic.Input(
      new TestMachine.Input.Activate(SecondaryState.Bopped)
    );
    // Repeating previous state should do nothing.
    logic.Input(
      new TestMachine.Input.Activate(SecondaryState.Bopped)
    );
    logic.Input(
      new TestMachine.Input.Activate(SecondaryState.Blooped)
    );
    logic.Input(
      new TestMachine.Input.Deactivate()
    );

    outputs.ShouldBe(new object[] {
      new TestMachine.Output.DeactivatedCleanUp(),
      new TestMachine.Output.Activated(),
      new TestMachine.Output.Blooped(),
      new TestMachine.Output.BloopedCleanUp(),
      new TestMachine.Output.ActivatedCleanUp(),
      new TestMachine.Output.Deactivated(),
      new TestMachine.Output.DeactivatedCleanUp(),
      new TestMachine.Output.Activated(),
      new TestMachine.Output.Bopped(),
      new TestMachine.Output.BoppedCleanUp(),
      new TestMachine.Output.Blooped(),
      new TestMachine.Output.BloopedCleanUp(),
      new TestMachine.Output.ActivatedCleanUp(),
      new TestMachine.Output.Deactivated(),
    });
  }

  [Fact]
  public void CallsEnterAndExitOnStatesInProperOrderForReusedStates() {
    var logic = new TestMachineReusable();
    var context = logic.Context;

    var outputs = new List<object>();

    void onOutput(object output) => outputs.Add(output);

    logic.OnOutput += onOutput;

    logic.Value.ShouldBeOfType<TestMachineReusable.State.Deactivated>();
    logic.Input(
      new TestMachineReusable.Input.Activate(SecondaryState.Blooped)
    );
    logic.Input(
      new TestMachineReusable.Input.Deactivate()
    );
    logic.Input(
      new TestMachineReusable.Input.Activate(SecondaryState.Bopped)
    );
    // Repeating previous state should do nothing.
    logic.Input(
      new TestMachineReusable.Input.Activate(SecondaryState.Bopped)
    );
    logic.Input(
      new TestMachineReusable.Input.Activate(SecondaryState.Blooped)
    );
    logic.Input(
      new TestMachineReusable.Input.Deactivate()
    );

    outputs.ShouldBe(new object[] {
      new TestMachineReusable.Output.DeactivatedCleanUp(),
      new TestMachineReusable.Output.Activated(),
      new TestMachineReusable.Output.Blooped(),
      new TestMachineReusable.Output.BloopedCleanUp(),
      new TestMachineReusable.Output.ActivatedCleanUp(),
      new TestMachineReusable.Output.Deactivated(),
      new TestMachineReusable.Output.DeactivatedCleanUp(),
      new TestMachineReusable.Output.Activated(),
      new TestMachineReusable.Output.Bopped(),
      new TestMachineReusable.Output.BoppedCleanUp(),
      new TestMachineReusable.Output.Blooped(),
      new TestMachineReusable.Output.BloopedCleanUp(),
      new TestMachineReusable.Output.ActivatedCleanUp(),
      new TestMachineReusable.Output.Deactivated(),
    });
  }

  [Fact]
  public void ReturnsCurrentValueIfProcessingInputs() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.DefaultContext(block);
    var called = false;
    var value = block.Input(new FakeLogicBlock.Input.InputCallback(
      () => {
        // This gets run from the input handler of InputCallback.
        var value = block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));
        value.ShouldBe(block.GetInitialState());
        called = true;
      },
      (context) => new FakeLogicBlock.State.StateA(2, 3)
    ));
    called.ShouldBeTrue();
  }

  [Fact]
  public void InvokesErrorEventFromUpdateHandler() {
    var block = new FakeLogicBlock();

    var called = 0;

    void handler(Exception e) => called++;

    block.OnError += handler;

    block.Input(new FakeLogicBlock.Input.Custom(
      (context) => new FakeLogicBlock.State.OnEnterState(
          context,
          (previous) =>
            throw new InvalidOperationException("Error from OnEnter")
        )
      )
    );

    called.ShouldBe(1);

    block.OnError -= handler;
  }

  [Fact]
  public void InvokesErrorEventFromUpdateHandlerManually() {
    var context = new FakeLogicBlock.FakeContext();
    var block = new FakeLogicBlock() {
      InitialState = () => new FakeLogicBlock.State.OnEnterState(
        context,
        (previous) =>
          throw new InvalidOperationException("Error from OnEnter")
      )
    };

    Should.Throw<InvalidOperationException>(
      () => block.Value.Enter()
    );
  }

  [Fact]
  public void StateCanAddInputUsingContext() {
    var logic = new FakeLogicBlock();
    var input = new FakeLogicBlock.Input.InputOne(5, 6);

    logic.Input(new FakeLogicBlock.Input.SelfInput(input));

    logic.Value.ShouldBeOfType<FakeLogicBlock.State.StateA>();
  }

  [Fact]
  public void StartsManuallyAndIgnoresStartWhenProcessing() {
    var enterCalled = false;
    var context = new FakeLogicBlock.FakeContext();
    var block = new FakeLogicBlock() {
      InitialState = () =>
        new FakeLogicBlock.State.OnEnterState(
          context, (previous) => enterCalled = true
        )
    };

    // LogicBlocks shouldn't call entrance handlers for the initial state.
    enterCalled.ShouldBeFalse();

    block.Value.Enter();

    enterCalled.ShouldBeTrue();
  }

  [Fact]
  public void StartEntersState() {
    var enterCalled = false;
    var context = new FakeLogicBlock.FakeContext();
    var block = new FakeLogicBlock() {
      InitialState = () =>
        new FakeLogicBlock.State.OnEnterState(
          context, (previous) => enterCalled = true
        )
    };

    enterCalled.ShouldBeFalse();

    block.Start();

    enterCalled.ShouldBeTrue();
  }

  [Fact]
  public void StopExitsState() {
    var exitCalled = false;
    var context = new FakeLogicBlock.FakeContext();
    var block = new FakeLogicBlock() {
      InitialState = () =>
        new FakeLogicBlock.State.OnExitState(
          context, (previous) => exitCalled = true
        )
    };

    exitCalled.ShouldBeFalse();

    block.Stop();

    exitCalled.ShouldBeTrue();
  }

  [Fact]
  public void CreatesFakeContext() {
    var inputs = new List<object> { "a", 2, true };
    var outputs = new List<object> { "b", 3, false };
    var errors = new List<Exception> {
      new InvalidOperationException(),
      new InvalidCastException()
    };
    var blackboard = new Dictionary<Type, object> {
      { typeof(string), "c" },
      { typeof(int), 4 },
      { typeof(bool), true }
    };

    var context = new FakeLogicBlock.FakeContext();

    context.Set(blackboard);
    context.Set(2d);

    inputs.ForEach((i) => context.Input(i));
    outputs.ForEach((o) => context.Output(o));
    errors.ForEach((e) => context.AddError(e));

    context.Inputs.ShouldBe(inputs);
    context.Outputs.ShouldBe(outputs);
    context.Errors.ShouldBe(errors);

    context.Get<string>().ShouldBe("c");
    context.Get<int>().ShouldBe(4);
    context.Get<bool>().ShouldBeTrue();
    context.Get<double>().ShouldBe(2d);

    Should.Throw<InvalidOperationException>(() => context.Get<float>());

    context.Reset();

    context.Inputs.ShouldBeEmpty();
    context.Outputs.ShouldBeEmpty();
    context.Errors.ShouldBeEmpty();

    Should.Throw<InvalidOperationException>(() => context.Get<string>());
  }
}
