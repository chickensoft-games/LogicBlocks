namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class SomeStateAsyncTest {
  [Fact]
  public async Task HandlesSomeInput() {
    var context = new Mock<MyLogicBlockAsync.IContext>(MockBehavior.Strict);
    var state = new MyLogicBlockAsync.State.SomeState(context.Object);

    var someOutputs = 0;
    // Expect our state to output SomeOutput when SomeInput is received.
    context
      .Setup(context => context.Output(
        new MyLogicBlockAsync.Output.SomeOutput())
      )
      .Callback(() => someOutputs++);

    // Perform the action we are testing on our state.
    var result = await state.On(new MyLogicBlockAsync.Input.SomeInput());

    // Make sure we got the next state.
    result.ShouldBeOfType<MyLogicBlockAsync.State.SomeOtherState>();

    // Create a special StateTester so we can run enter/exit callbacks.
    var stateTester = MyLogicBlockAsync.Test(state);

    // Simulate enter/exit callbacks
    await stateTester.Enter();
    await stateTester.Exit();

    // Make sure we got 3 outputs:
    // 1 from enter, 1 from input handler, and 1 from exit.
    someOutputs.ShouldBe(3);

    // Make sure the output we expected was produced by ensuring our mock
    // context was called the same way we set it up.
    context.VerifyAll();
  }
}
