namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class LogicStateLogicTest {
  [Fact]
  public void Initializes() {
    var logic = new TestMachine();
    var stateLogic = new TestMachine.State.Deactivated();
    stateLogic.GetHashCode().ShouldBeOfType<int>();
  }
}
