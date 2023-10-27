namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class SomeStateTest {
  [Fact]
  public void SomeStateEnters() {
    var context = MyLogicBlock.CreateFakeContext();

    var state = new MyLogicBlock.State.SomeState(context);

    state.Enter();

    context.Outputs.ShouldBe(
      new object[] { new MyLogicBlock.Output.SomeOutput() }
    );
  }

  [Fact]
  public void SomeStateExits() {
    var context = MyLogicBlock.CreateFakeContext();

    var state = new MyLogicBlock.State.SomeState(context);

    state.Exit();

    context.Outputs.ShouldBe(
      new object[] { new MyLogicBlock.Output.SomeOutput() }
    );
  }

  [Fact]
  public void GoesToSomeOtherStateOnSomeInput() {
    var context = MyLogicBlock.CreateFakeContext();

    var state = new MyLogicBlock.State.SomeState(context);

    var nextState = state.On(new MyLogicBlock.Input.SomeInput());

    nextState.ShouldBeOfType<MyLogicBlock.State.SomeOtherState>();

    context.Outputs.ShouldBe(
      new object[] { new MyLogicBlock.Output.SomeOutput() }
    );
  }
}
