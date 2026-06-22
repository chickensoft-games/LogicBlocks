namespace Chickensoft.LogicBlocks.Tutorial;

using Moq;
using Shouldly;
using Xunit;
using static Chickensoft.LogicBlocks.Tutorial.Timer;

public class TimerTest
{
  private sealed record MyComponent(ITimer Timer)
  {
    public void DoSomething()
    {
      if (Timer.State is TimerState.PoweredOff)
      {
        // Do something when the timer is off.
      }
    }
  }

  [Fact]
  public void Mocks()
  {
    var timer = new Mock<ITimer>();

    // Make the mock logic block be in a specific state.
    timer.Setup(t => t.State).Returns(new TimerState.PoweredOff());

    var component = new MyComponent(timer.Object);
    component.DoSomething();
  }

  [Fact]
  public void Binds()
  {
    var timer = new Timer();
    using var binding = timer.Bind();

    binding.OnInput((in TimerState.Input.ChangeDuration input) =>
    {
      // Watch for duration change inputs.
    });

    binding.OnOutput((in TimerState.Output.PlayBeepingSound output) =>
    {
      // Play a beeping sound.
    });

    binding.OnState<TimerState.PoweredOn.Idle>(state =>
    {
      // Do something when the timer becomes idle.
    });
  }

  [Fact]
  public void Initializes()
  {
    // Mock dependencies that the logic block needs.
    var clock = new Mock<IClock>();

    var timer = new Timer();

    // Add the mocked dependencies to the blackboard.
    timer.Set(clock.Object);

    timer.Start<TimerState.PoweredOff>();

    // Check that the initial state is the one we expect.
    timer.State
      .ShouldBeOfType<TimerState.PoweredOff>();

    // Verify the timer has set its blackboard data correctly.
    timer.Has<Data>().ShouldBeTrue();
    timer.Get<IClock>().ShouldBe(clock.Object);
  }
}

public class PoweredOnStateTest()
{
  // We can't create abstract classes directly for testing, so we extend it.
  public record TestPoweredOnState : TimerState.PoweredOn { }

  [Fact]
  public void TurnsOff()
  {
    var state = new TestPoweredOnState();

    state.Test();

    state.On(new TimerState.Input.PowerButtonPressed())
      .IsAssignableTo(typeof(TimerState.PoweredOff));
  }
}

public class IdleStateTest()
{
  [Fact]
  public void ChangesDuration()
  {
    var state = new TimerState.PoweredOn.Idle();
    var tester = state.Test();

    tester.Set(new Data() { Duration = 30.0d });

    var duration = 45;

    state.On(new TimerState.Input.ChangeDuration(duration))
      .IsAssignableTo(typeof(TimerState.PoweredOn.Idle));

    tester.Get<Data>().Duration.ShouldBe(duration);
  }

  [Fact]
  public void Starts()
  {
    var state = new TimerState.PoweredOn.Idle();
    _ = state.Test();

    state.On(new TimerState.Input.StartStopButtonPressed())
      .IsAssignableTo(typeof(TimerState.PoweredOn.Countdown));
  }
}

public class CountdownStateTest
{
  [Fact]
  public void AddsTimeElapsedInputWhenClockFires()
  {
    var state = new TimerState.PoweredOn.Countdown();
    var tester = state.Test();

    var clock = new Mock<IClock>();
    tester.Set(clock.Object);

    state.Enter();

    clock.Raise(c => c.TimeElapsed += null, 1.0d);

    // Make sure state unsubscribes.
    state.Exit();

    // Fire event again to make sure it doesn't get added.
    clock.Raise(c => c.TimeElapsed += null, 1.0d);

    tester.Inputs.ShouldBe([new TimerState.Input.TimeElapsed(1.0d)]);
  }

  [Fact]
  public void OnTimeElapsedStartsBeeping()
  {
    var state = new TimerState.PoweredOn.Countdown();
    var tester = state.Test();

    tester.Set(new Data() { TimeRemaining = 1.0d });

    state.On(new TimerState.Input.TimeElapsed(1.0d))
      .IsAssignableTo(typeof(TimerState.PoweredOn.Beeping));
  }

  [Fact]
  public void OnTimeElapsedKeepsCountingDown()
  {
    var state = new TimerState.PoweredOn.Countdown();
    var tester = state.Test();

    tester.Set(new Data() { TimeRemaining = 2.0d });

    state.On(new TimerState.Input.TimeElapsed(1.0d))
      .IsAssignableTo(typeof(TimerState.PoweredOn.Countdown));

    tester.Get<Data>().TimeRemaining.ShouldBe(1.0d);
  }

  [Fact]
  public void OnStartStopButtonPressedIdles()
  {
    var state = new TimerState.PoweredOn.Countdown();
    _ = state.Test();

    state.On(new TimerState.Input.StartStopButtonPressed())
      .IsAssignableTo(typeof(TimerState.PoweredOn.Idle));
  }
}

public class BeepingStateTest
{
  [Fact]
  public void PlaysBeepingSoundOnEnter()
  {
    var state = new TimerState.PoweredOn.Beeping();
    var tester = state.Test();

    state.Enter();

    tester.Outputs.ShouldBe([new TimerState.Output.PlayBeepingSound()]);
  }

  [Fact]
  public void StopsBeepingSoundOnExit()
  {
    var state = new TimerState.PoweredOn.Beeping();
    var tester = state.Test();

    state.Exit();

    tester.Outputs.ShouldBe([new TimerState.Output.StopBeepingSound()]);
  }

  [Fact]
  public void OnStartStopButtonPressedIdles()
  {
    var state = new TimerState.PoweredOn.Beeping();
    _ = state.Test();

    state.On(new TimerState.Input.StartStopButtonPressed())
      .IsAssignableTo(typeof(TimerState.PoweredOn.Idle));
  }
}

public class PoweredOffStateTest()
{
  [Fact]
  public void TurnsOn()
  {
    var state = new TimerState.PoweredOff();
    _ = state.Test();

    state.On(new TimerState.Input.PowerButtonPressed())
      .IsAssignableTo(typeof(TimerState.PoweredOn.Idle));
  }
}
