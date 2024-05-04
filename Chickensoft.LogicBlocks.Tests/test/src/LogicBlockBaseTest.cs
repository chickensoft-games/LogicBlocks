namespace Chickensoft.LogicBlocks.Tests;

using Shouldly;
using Xunit;

public class LogicBlockBaseTest {
  private sealed record TestValue(int Value);

  [Fact]
  public void IsEquivalent() {
    LogicBlockBase.IsEquivalent(null, null).ShouldBeTrue();
    LogicBlockBase.IsEquivalent(null, new object()).ShouldBeFalse();
    var obj = new object();
    // same instance
    LogicBlockBase.IsEquivalent(obj, obj).ShouldBeTrue();

    var a = new TestValue(1);
    var b = new TestValue(1);
    // different instance but equivalent
    LogicBlockBase.IsEquivalent(a, b).ShouldBeTrue();
  }
}
