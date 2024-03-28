namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;
using static Chickensoft.LogicBlocks.Tests.Fixtures.Timer;

public class TimerTest {
  [Fact]
  public void Initializes() {
    var clock = new Mock<IClock>();
    var timer = new Timer(clock.Object);

    var state = timer.GetInitialState();

    // Verify the timer starts in the expected state.
    state.ShouldBeOfType<State.PoweredOff>();

    // Verify the timer has set its blackboard data correctly.
    timer.Get<Data>().ShouldNotBeNull();
    timer.Get<IClock>().ShouldBe(clock.Object);
  }
}

public class TimerPoweredOffStateTest() {
  [Fact]
  public void TurnsOn() {
    var state = new State.PoweredOff();
    var context = state.CreateFakeContext();

    var idle = new Mock<State.PoweredOn.IIdle>();
    context.Set(idle.Object);

    state.On(new Input.PowerButtonPressed()).ShouldBeSameAs(idle.Object);
  }
}
