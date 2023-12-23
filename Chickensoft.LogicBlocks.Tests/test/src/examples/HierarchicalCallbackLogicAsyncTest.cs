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
        () => new HierarchicalCallbackLogicAsync.State.Substate()
    };

    var context = new Mock<IContext>();

    await callbackLogic.Value.Enter(
      new Mock<HierarchicalCallbackLogicAsync.State>().Object
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
        () => new HierarchicalCallbackLogicAsync.State.Substate()
    };

    var context = new Mock<IContext>();

    await callbackLogic.Value.Exit(
      new Mock<HierarchicalCallbackLogicAsync.State>().Object
    );
    log.ShouldBe(new List<string> { "substate" });

    log.Clear();

    await callbackLogic.Value.Exit();
    log.ShouldBe(new List<string> { "substate", "state" });
  }
}
