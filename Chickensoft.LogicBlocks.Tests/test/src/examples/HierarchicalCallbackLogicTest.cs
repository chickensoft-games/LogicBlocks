namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class HierarchicalCallbackLogicTest {
  [Fact]
  public void StateCallsRelevantEntranceCallbacks() {
    var log = new List<string>();
    var callbackLogic = new HierarchicalCallbackLogic(log) {
      InitialState =
        (context) => new HierarchicalCallbackLogic.State.Substate(context)
    };

    var context = new Mock<HierarchicalCallbackLogic.IContext>();

    callbackLogic.Value.Enter(
      new Mock<HierarchicalCallbackLogic.State>(context.Object).Object
    );
    log.ShouldBe(new List<string> { "substate" });

    log.Clear();

    callbackLogic.Value.Enter();
    log.ShouldBe(new List<string> { "state", "substate" });
  }

  [Fact]
  public void StateCallsRelevantExitCallbacks() {
    var log = new List<string>();
    var callbackLogic = new HierarchicalCallbackLogic(log) {
      InitialState =
        (context) => new HierarchicalCallbackLogic.State.Substate(context)
    };

    var context = new Mock<HierarchicalCallbackLogic.IContext>();

    callbackLogic.Value.Exit(
      new Mock<HierarchicalCallbackLogic.State>(context.Object).Object
    );
    log.ShouldBe(new List<string> { "substate" });

    log.Clear();

    callbackLogic.Value.Exit();
    log.ShouldBe(new List<string> { "substate", "state" });
  }
}
