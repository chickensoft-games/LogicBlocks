namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class SomeStateTest {
  [Fact]
  public void HandlesSomeInput() {
    var context = new Mock<MyLogicBlock.IContext>(MockBehavior.Strict);
    var state = new MyLogicBlock.State.SomeState(context.Object);

    var someOutputs = 0;
    // Expect our state to output SomeOutput when SomeInput is received.
    context
      .Setup(context => context.Output(
        It.Ref<MyLogicBlock.Output.SomeOutput>.IsAny
      )).Callback(() => someOutputs++);

    // Perform the action we are testing on our state.
    var result = state.On(new MyLogicBlock.Input.SomeInput());

    // Make sure we got the next state.
    result.ShouldBeOfType<MyLogicBlock.State.SomeOtherState>();

    // Create a special StateTester so we can run enter/exit callbacks.
    var stateTester = MyLogicBlock.Test(state);

    // Simulate enter/exit callbacks
    stateTester.Enter();
    stateTester.Exit();

    // Make sure we got 3 outputs:
    // 1 from enter, 1 from input handler, and 1 from exit.
    someOutputs.ShouldBe(3);

    // Make sure the output we expected was produced by ensuring our mock
    // context was called the same way we set it up.
    context.VerifyAll();
  }
}
