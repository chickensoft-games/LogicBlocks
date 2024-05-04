namespace Chickensoft.Introspection;

using Xunit;

public class MixinBlackboardTest {
  [Fact]
  public void IsAlwaysEqualToAnything() {
    var blackboard = new MixinBlackboard();
    Assert.True(blackboard.Equals(null!));
    Assert.True(blackboard!.Equals(new object()));
    Assert.True(blackboard.Equals(blackboard));
  }
}
