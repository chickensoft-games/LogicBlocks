namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class FakeContextTest
{
  [Fact]
  public void OverridesEqualityToAlwaysBeEqual()
  {
    var context1 = new FakeContext();
    var context2 = new FakeContext();

    context1.ShouldBeEquivalentTo(context2);
    context1.GetHashCode().ShouldNotBe(context2.GetHashCode());
  }

  [Fact]
  public void CreatesStateIfNeeded()
  {
    var context = new FakeContext();

    // Fake context can just create a state if there isn't one on the
    // blackboard. Saves a lot of time and trouble when testing.
    context.Get<MyLogicBlock.State.SomeState>().ShouldNotBeNull();
  }
}
