namespace Chickensoft.LogicBlocks.Tests;

using System.Collections.Generic;
using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class AbstractTransitionBlockTest
{
  [Fact]
  public void DoesNotTransitionToAbstractState()
  {
    var block = new AbstractTransitionBlock();

    Should.Throw<KeyNotFoundException>(
      () => block.Input(new AbstractTransitionBlock.Input.Signal())
    );
  }
}
