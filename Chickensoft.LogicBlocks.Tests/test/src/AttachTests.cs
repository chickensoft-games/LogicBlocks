namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class AttachTests {
  [Fact]
  public void MultipleInputsOnAttachAreQueued() {
    var logic = new GreedyLogic();

    Should.NotThrow(() => logic.Start());
  }
}
