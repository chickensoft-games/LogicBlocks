namespace Chickensoft.LogicBlocks.Tests;

using System;
using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class DefaultContextTest
{
  [Fact]
  public void OverridesEqualityToAlwaysBeEqual()
  {
    var context1 = new FakeLogicBlock.DefaultContext();
    var context2 = new FakeLogicBlock.DefaultContext();

    context1.ShouldBeEquivalentTo(context2);
    context1.GetHashCode().ShouldBe(context2.GetHashCode());
  }
}

public class ContextAdapterTest
{
  [Fact]
  public void OverridesEqualityToAlwaysBeEqual()
  {
    var context1 = new FakeLogicBlock.ContextAdapter();
    var context2 = new FakeLogicBlock.ContextAdapter();

    context1.Equals(context2).ShouldBeTrue();
    context1.GetHashCode().ShouldBe(context2.GetHashCode());
  }

  [Fact]
  public void InputThrowsWhenNoContextIsSet()
  {
    var context = new FakeLogicBlock.ContextAdapter();

    Should.Throw<InvalidOperationException>(() => context.Input(1));
  }

  [Fact]
  public void OutputThrowsWhenNoContextIsSet()
  {
    var context = new FakeLogicBlock.ContextAdapter();

    Should.Throw<InvalidOperationException>(() => context.Output(1));
  }

  [Fact]
  public void GetThrowsWhenNoContextIsSet()
  {
    var context = new FakeLogicBlock.ContextAdapter();

    Should.Throw<InvalidOperationException>(context.Get<string>);
  }

  [Fact]
  public void AddErrorThrowsWhenNoContextIsSet()
  {
    var context = new FakeLogicBlock.ContextAdapter();

    Should.Throw<InvalidOperationException>(
      () => context.AddError(new InvalidOperationException())
    );
  }
}
