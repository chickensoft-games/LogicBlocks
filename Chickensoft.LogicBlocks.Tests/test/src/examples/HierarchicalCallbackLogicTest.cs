namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class HierarchicalCallbackLogicTest
{
  private static HierarchicalCallbackLogic.Output.Log Log(string message) =>
    new(message);

  [Fact]
  public void StateCallsRelevantEntranceCallbacks()
  {
    var state = new HierarchicalCallbackLogic.State.Substate();
    var context = state.CreateFakeContext();

    state.Enter(
      new Mock<HierarchicalCallbackLogic.State>().Object
    );
    context.Outputs.ShouldBe([Log("substate")]);

    context.Reset();

    state.Enter();
    context.Outputs.ShouldBe([Log("state"), Log("substate")]);
  }

  [Fact]
  public void StateCallsRelevantExitCallbacks()
  {
    var state = new HierarchicalCallbackLogic.State.Substate();
    var context = state.CreateFakeContext();

    state.Exit(
      new Mock<HierarchicalCallbackLogic.State>().Object
    );
    context.Outputs.ShouldBe([Log("substate")]);

    context.Reset();

    state.Exit();
    context.Outputs.ShouldBe([Log("substate"), Log("state")]);
  }
}
