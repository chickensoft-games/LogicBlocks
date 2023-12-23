namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class SomeStateAsyncTest {
  [Fact]
  public async Task SomeStateEnters() {
    var state = new MyLogicBlockAsync.State.SomeState();
    var context = state.CreateFakeContext();

    await state.Enter();

    context.Outputs.ShouldBe(
      new object[] { new MyLogicBlockAsync.Output.SomeOutput() }
    );
  }

  [Fact]
  public async Task SomeStateExits() {
    var state = new MyLogicBlockAsync.State.SomeState();
    var context = state.CreateFakeContext();

    await state.Exit();

    context.Outputs.ShouldBe(
      new object[] { new MyLogicBlockAsync.Output.SomeOutput() }
    );
  }

  [Fact]
  public async Task GoesToSomeOtherStateOnSomeInput() {
    var state = new MyLogicBlockAsync.State.SomeState();
    var context = state.CreateFakeContext();

    var nextState = await state.On(new MyLogicBlockAsync.Input.SomeInput());

    nextState.ShouldBeOfType<MyLogicBlockAsync.State.SomeOtherState>();

    context.Outputs.ShouldBe(
      new object[] { new MyLogicBlockAsync.Output.SomeOutput() }
    );
  }
}
