namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class HierarchicalCallbackLogicAsyncTest {
  [Fact]
  public async Task StateCallsRelevantEntranceCallbacks() {
    var log = new List<string>();
    var callbackLogic = new HierarchicalCallbackLogicAsync(log) {
      InitialState =
        (context) => new HierarchicalCallbackLogicAsync.State.Substate(context)
    };

    var context = new Mock<HierarchicalCallbackLogicAsync.IContext>();

    await callbackLogic.Value.Enter(
      new Mock<HierarchicalCallbackLogicAsync.State>(context.Object).Object
    );
    log.ShouldBe(new List<string> { "substate" });

    log.Clear();

    await callbackLogic.Value.Enter();
    log.ShouldBe(new List<string> { "state", "substate" });
  }

  [Fact]
  public async Task StateCallsRelevantExitCallbacks() {
    var log = new List<string>();
    var callbackLogic = new HierarchicalCallbackLogicAsync(log) {
      InitialState =
        (context) => new HierarchicalCallbackLogicAsync.State.Substate(context)
    };

    var context = new Mock<HierarchicalCallbackLogicAsync.IContext>();

    await callbackLogic.Value.Exit(
      new Mock<HierarchicalCallbackLogicAsync.State>(context.Object).Object
    );
    log.ShouldBe(new List<string> { "substate" });

    log.Clear();

    await callbackLogic.Value.Exit();
    log.ShouldBe(new List<string> { "substate", "state" });
  }
}
