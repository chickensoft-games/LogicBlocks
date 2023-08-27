namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class SomeStateTest {
  [Fact]
  public void HandlesSomeInput() {
    var context = new Mock<MyLogicBlock.IContext>();
    var state = new MyLogicBlock.State.SomeState(context.Object);

    // Expect our state to output SomeOutput when SomeInput is received.
    context
      .Setup(context => context.Output(new MyLogicBlock.Output.SomeOutput()));

    // Perform the action we are testing on our state.
    var result = state.On(new MyLogicBlock.Input.SomeInput());

    // Make sure the output we expected was produced by ensuring our mock
    // context was called the same way we set it up.
    context.VerifyAll();

    // Make sure we got the next state.
    result.ShouldBeOfType<MyLogicBlock.State.SomeOtherState>();
  }
}
