namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class LogicBlockTest {
  [Fact]
  public void Initializes() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.Context(block);
    block.Value.ShouldBe(block.GetInitialState(context));
  }

  [Fact]
  public void GetsAndSetsBlackboardData() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.Context(block);
    block.PublicSet("data");
    block.Get<string>().ShouldBe("data");

    // Can't change values once set.
    Should.Throw<ArgumentException>(() => block.PublicSet("other"));
    Should.Throw<KeyNotFoundException>(() => block.Get<int>());
    block.Input(new FakeLogicBlock.Input.GetString());
    block.Value.ShouldBe(new FakeLogicBlock.State.StateC(context, "data"));
  }

  [Fact]
  public void InvokesInputEvent() {
    var block = new FakeLogicBlock();

    var called = 0;
    var input = new FakeLogicBlock.Input.InputOne(2, 3);

    void handler(object? block, FakeLogicBlock.Input input) {
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

    void handler(object? block, FakeLogicBlock.Output output) {
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
  public void InvokesNextStateEvent() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.Context(block);

    var called = 0;
    var state = new FakeLogicBlock.State.StateA(context, 2, 3);

    void handler(object? block, FakeLogicBlock.State state) {
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

    void handler(object? _, Exception e) => called++;

    block.OnError += handler;

    block.Input(new FakeLogicBlock.Input.InputError());

    called.ShouldBe(1);

    block.OnError -= handler;
  }

  [Fact]
  public void DoesNothingOnUnhandledInput() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.Context(block);
    block.Input(new FakeLogicBlock.Input.InputUnknown());
    block.Value.ShouldBe(block.GetInitialState(context));
  }

  [Fact]
  public void CallsEnterAndExitOnStatesInProperOrder() {
    var logic = new TestMachine();
    var context = new TestMachine.Context(logic);

    var outputs = new List<TestMachine.Output>();

    void onOutput(object? block, TestMachine.Output output) =>
      outputs.Add(output);

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

    outputs.ShouldBe(new TestMachine.Output[] {
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

    var outputs = new List<TestMachineReusable.Output>();

    void onOutput(object? block, TestMachineReusable.Output output) =>
      outputs.Add(output);

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

    outputs.ShouldBe(new TestMachineReusable.Output[] {
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
    var context = new FakeLogicBlock.Context(block);
    var called = false;
    var value = block.Input(new FakeLogicBlock.Input.InputCallback(
      () => {
        // This gets run from the input handler of InputCallback.
        var value = block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));
        value.ShouldBe(block.GetInitialState(context));
        called = true;
      },
      (context) => new FakeLogicBlock.State.StateA(context, 2, 3)
    ));
    called.ShouldBeTrue();
  }

  [Fact]
  public void InvokesErrorEventFromUpdateHandler() {
    var block = new FakeLogicBlock();

    var called = 0;

    void handler(object? _, Exception e) => called++;

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
  public void DeterminesIfCanChangeStateToNonEquatableState() {
    var logic = new NonEquatable();
    logic.Value.ShouldBeOfType<NonEquatable.State.A>();
    logic.Input(new NonEquatable.Input.GoToA());
    logic.Value.ShouldBeOfType<NonEquatable.State.A>();
    logic.Input(new NonEquatable.Input.GoToB());
    logic.Value.ShouldBeOfType<NonEquatable.State.B>();
  }

  [Fact]
  public void OnTransitionCalledWhenTransitioning() {
    var logic = new FakeLogicBlock();
    var called = false;

    logic.PublicOnTransition<
      FakeLogicBlock.State.StateA, FakeLogicBlock.State.StateB
    >(
      (previous, state) => {
        previous.ShouldBeOfType<FakeLogicBlock.State.StateA>();
        state.ShouldBeOfType<FakeLogicBlock.State.StateB>();
        called = true;
      }
    );

    logic.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    Should.Throw<ArgumentException>(
      () => logic.PublicOnTransition<
        FakeLogicBlock.State.StateA, FakeLogicBlock.State.StateB
      >((previous, state) => { })
    );

    called.ShouldBeTrue();
  }

  [Fact]
  public void StateCanAddInputUsingContext() {
    var logic = new FakeLogicBlock();
    var input = new FakeLogicBlock.Input.InputOne(5, 6);

    logic.Input(new FakeLogicBlock.Input.SelfInput(input));

    logic.Value.ShouldBeOfType<FakeLogicBlock.State.StateA>();
  }
}
