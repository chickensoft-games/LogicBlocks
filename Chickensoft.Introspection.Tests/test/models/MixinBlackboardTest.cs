namespace Chickensoft.Introspection.Tests.Models;

using Shouldly;
using Xunit;

public class MixinBlackboardTest {
  [Fact]
  public void IsAlwaysEqualToAnything() {
    var blackboard = new MixinBlackboard();
    blackboard.Equals(null!).ShouldBeTrue();
    blackboard!.Equals(new object()).ShouldBeTrue();
    blackboard.Equals(blackboard).ShouldBeTrue();

    blackboard.GetHashCode().ShouldBeOfType<int>();
  }
}
