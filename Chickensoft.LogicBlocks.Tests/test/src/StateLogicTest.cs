namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public partial class LogicStateLogicTest {
  [Fact]
  public void Initializes() {
    var logic = new TestMachine();
    var stateLogic = new TestMachine.State.Deactivated();
    stateLogic.GetHashCode().ShouldBeOfType<int>();
  }

  [Fact]
  public void EnterWithTypeRespectsType() {
    var state = new TestMachine.State.Activated.Blooped();
    var context = state.CreateFakeContext();

    // Enter, but pretend we're in Activated already. Only Blooped's entrance
    // callbacks should run.
    state.Enter<TestMachine.State.Activated>();

    context.Outputs.ShouldBe([
      new TestMachine.Output.Blooped()
    ]);
  }

  [Fact]
  public void ExitWithTypeRespectsType() {
    var state = new TestMachine.State.Activated.Blooped();
    var context = state.CreateFakeContext();

    // Exit, but pretend we're going to another Activated state.
    // Only the Blooped's exit callbacks should run.
    state.Exit<TestMachine.State.Activated>();

    context.Outputs.ShouldBe([
      new TestMachine.Output.BloopedCleanUp()
    ]);
  }
}
