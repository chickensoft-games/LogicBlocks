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
        It.IsAny<MyLogicBlock.Output.SomeOutput>()
      )).Callback(() => someOutputs++);

    // Perform the action we are testing on our state.
    // In this case, we are testing the input handler for SomeInput
    var result = state.On(new MyLogicBlock.Input.SomeInput());

    // Make sure the input handler returned the correct next state.
    result.ShouldBeOfType<MyLogicBlock.State.SomeOtherState>();

    // Simulate enter/exit callbacks.
    state.Enter();
    state.Exit();

    // Make sure we got 3 outputs:
    // 1 from input handler, 1 from enter, and 1 from exit.
    someOutputs.ShouldBe(3);

    // Make sure the output we expected was produced by ensuring our mock
    // context was called the same way we set it up.
    context.VerifyAll();
  }
}
