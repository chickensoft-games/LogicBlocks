namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class SomeStateTest {
  [Fact]
  public void SomeStateEnters() {
    var state = new MyLogicBlock.State.SomeState();
    var context = state.CreateFakeContext();

    state.Enter();

    context.Outputs.ShouldBe([new MyLogicBlock.Output.SomeOutput()]);
  }

  [Fact]
  public void SomeStateExits() {
    var state = new MyLogicBlock.State.SomeState();
    var context = state.CreateFakeContext();

    state.Exit();

    context.Outputs.ShouldBe([new MyLogicBlock.Output.SomeOutput()]);
  }

  [Fact]
  public void GoesToSomeOtherStateOnSomeInput() {
    var state = new MyLogicBlock.State.SomeState();
    var context = state.CreateFakeContext();

    var nextState = state.On(new MyLogicBlock.Input.SomeInput());

    nextState.ShouldBeOfType<MyLogicBlock.State.SomeOtherState>();

    context.Outputs.ShouldBe([new MyLogicBlock.Output.SomeOutput()]);
  }
}
