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
      if (Timer.State is Timer.TimerState.PoweredOff)
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
    timer.Setup(t => t.State).Returns(new Timer.TimerState.PoweredOff());

    var component = new MyComponent(timer.Object);
    component.DoSomething();
  }

  [Fact]
  public void Binds()
  {
    var timer = new Timer();
    using var binding = timer.Bind();

    binding.OnInput((in Input.ChangeDuration input) =>
    {
      // Watch for duration change inputs.
    });

    binding.OnOutput((in Output.PlayBeepingSound output) =>
    {
      // Play a beeping sound.
    });

    binding.OnState<Timer.TimerState.PoweredOn.Idle>(state =>
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

    timer.Start<Timer.TimerState.PoweredOff>();

    // Check that the initial state is the one we expect.
    timer.State
      .ShouldBeOfType<Timer.TimerState.PoweredOff>();

    // Verify the timer has set its blackboard data correctly.
    timer.Has<Data>().ShouldBeTrue();
    timer.Get<IClock>().ShouldBe(clock.Object);
  }
}

public class PoweredOnStateTest()
{
  // We can't create abstract classes directly for testing, so we extend it.
  public record TestPoweredOnState : Timer.TimerState.PoweredOn { }

  [Fact]
  public void TurnsOff()
  {
    var state = new TestPoweredOnState();

    state.Test();

    state.On(new Input.PowerButtonPressed())
      .IsAssignableTo(typeof(Timer.TimerState.PoweredOff));
  }
}

public class IdleStateTest()
{
  [Fact]
  public void ChangesDuration()
  {
    var state = new Timer.TimerState.PoweredOn.Idle();
    var tester = state.Test();

    tester.Set(new Data() { Duration = 30.0d });

    var duration = 45;

    state.On(new Input.ChangeDuration(duration))
      .IsAssignableTo(typeof(Timer.TimerState.PoweredOn.Idle));

    tester.Get<Data>().Duration.ShouldBe(duration);
  }

  [Fact]
  public void Starts()
  {
    var state = new Timer.TimerState.PoweredOn.Idle();
    _ = state.Test();

    state.On(new Input.StartStopButtonPressed())
      .IsAssignableTo(typeof(Timer.TimerState.PoweredOn.Countdown));
  }
}

public class CountdownStateTest
{
  [Fact]
  public void AddsTimeElapsedInputWhenClockFires()
  {
    var state = new Timer.TimerState.PoweredOn.Countdown();
    var tester = state.Test();

    var clock = new Mock<IClock>();
    tester.Set(clock.Object);

    state.Enter();

    clock.Raise(c => c.TimeElapsed += null, 1.0d);

    // Make sure state unsubscribes.
    state.Exit();

    // Fire event again to make sure it doesn't get added.
    clock.Raise(c => c.TimeElapsed += null, 1.0d);

    tester.Inputs.ShouldBe([new Input.TimeElapsed(1.0d)]);
  }

  [Fact]
  public void OnTimeElapsedStartsBeeping()
  {
    var state = new Timer.TimerState.PoweredOn.Countdown();
    var tester = state.Test();

    tester.Set(new Data() { TimeRemaining = 1.0d });

    state.On(new Input.TimeElapsed(1.0d))
      .IsAssignableTo(typeof(Timer.TimerState.PoweredOn.Beeping));
  }

  [Fact]
  public void OnTimeElapsedKeepsCountingDown()
  {
    var state = new Timer.TimerState.PoweredOn.Countdown();
    var tester = state.Test();

    tester.Set(new Data() { TimeRemaining = 2.0d });

    state.On(new Input.TimeElapsed(1.0d))
      .IsAssignableTo(typeof(Timer.TimerState.PoweredOn.Countdown));

    tester.Get<Data>().TimeRemaining.ShouldBe(1.0d);
  }

  [Fact]
  public void OnStartStopButtonPressedIdles()
  {
    var state = new Timer.TimerState.PoweredOn.Countdown();
    _ = state.Test();

    state.On(new Input.StartStopButtonPressed())
      .IsAssignableTo(typeof(Timer.TimerState.PoweredOn.Idle));
  }
}

public class BeepingStateTest
{
  [Fact]
  public void PlaysBeepingSoundOnEnter()
  {
    var state = new Timer.TimerState.PoweredOn.Beeping();
    var tester = state.Test();

    state.Enter();

    tester.Outputs.ShouldBe([new Output.PlayBeepingSound()]);
  }

  [Fact]
  public void StopsBeepingSoundOnExit()
  {
    var state = new Timer.TimerState.PoweredOn.Beeping();
    var tester = state.Test();

    state.Exit();

    tester.Outputs.ShouldBe([new Output.StopBeepingSound()]);
  }

  [Fact]
  public void OnStartStopButtonPressedIdles()
  {
    var state = new Timer.TimerState.PoweredOn.Beeping();
    _ = state.Test();

    state.On(new Input.StartStopButtonPressed())
      .IsAssignableTo(typeof(Timer.TimerState.PoweredOn.Idle));
  }
}

public class PoweredOffStateTest()
{
  [Fact]
  public void TurnsOn()
  {
    var state = new Timer.TimerState.PoweredOff();
    _ = state.Test();

    state.On(new Input.PowerButtonPressed())
      .IsAssignableTo(typeof(Timer.TimerState.PoweredOn.Idle));
  }
}
