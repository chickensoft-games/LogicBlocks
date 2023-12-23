namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class FakeContextTest {
  [Fact]
  public void OverridesEqualityToAlwaysBeEqual() {
    var context1 = new FakeLogicBlock.FakeContext();
    var context2 = new FakeLogicBlock.FakeContext();

    context1.ShouldBeEquivalentTo(context2);
    context1.GetHashCode().ShouldNotBe(context2.GetHashCode());
  }
}
