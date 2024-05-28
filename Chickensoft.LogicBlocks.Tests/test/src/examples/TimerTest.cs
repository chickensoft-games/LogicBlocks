namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Examples;
using Moq;
using Shouldly;
using Xunit;
using static Chickensoft.LogicBlocks.Tests.Examples.Timer;

public class TimerTest {
  [Fact]
  public void Initializes() {
    var clock = new Mock<IClock>();
    var timer = new Timer();
    timer.Set(clock.Object);

    var state = timer.GetInitialState().State;

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

    context.Set(new State.PoweredOn.Idle());

    state.On(new Input.PowerButtonPressed()).State
      .ShouldBeOfType<State.PoweredOn.Idle>();
  }
}
