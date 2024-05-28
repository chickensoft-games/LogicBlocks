namespace Chickensoft.Serialization.Tests;

using Chickensoft.Serialization;
using Shouldly;
using Xunit;

public class UtilitiesTest {
  private sealed record TestValue(int Value);

  [Fact]
  public void IsEquivalent() {
    SerializationUtilities.IsEquivalent(null, null).ShouldBeTrue();
    SerializationUtilities.IsEquivalent(null, new object()).ShouldBeFalse();
    var obj = new object();
    // same instance
    SerializationUtilities.IsEquivalent(obj, obj).ShouldBeTrue();

    var a = new TestValue(1);
    var b = new TestValue(1);
    // different instance but equivalent
    SerializationUtilities.IsEquivalent(a, b).ShouldBeTrue();
  }
}
