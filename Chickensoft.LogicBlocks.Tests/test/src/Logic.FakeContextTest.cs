namespace Chickensoft.LogicBlocks.Tests;

using Shouldly;
using Xunit;

public class FakeContextTest {
  [Fact]
  public void OverridesEqualityToAlwaysBeEqual() {
    var context1 = new FakeContext();
    var context2 = new FakeContext();

    context1.ShouldBeEquivalentTo(context2);
    context1.GetHashCode().ShouldNotBe(context2.GetHashCode());
  }
}
