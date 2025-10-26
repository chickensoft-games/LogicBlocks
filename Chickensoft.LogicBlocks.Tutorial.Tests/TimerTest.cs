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
      if (Timer.Value is State.PoweredOff)
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
    timer.Setup(t => t.Value).Returns(new State.PoweredOff());

    var component = new MyComponent(timer.Object);
    component.DoSomething();
  }

  [Fact]
  public void Binds()
  {
    var timer = new Timer();
    using var binding = timer.Bind();

    binding.Watch((in Input.ChangeDuration input) =>
    {
      // Watch for duration change inputs.
    });

    binding.Handle((in Output.PlayBeepingSound output) =>
    {
      // Play a beeping sound.
    });

    binding.When<State.PoweredOn.Idle>(state =>
    {
      // Do something when the timer becomes idle.
    });

    binding.Catch<System.Exception>(e => { });
  }

  [Fact]
  public void Initializes()
  {
    // Mock dependencies that the logic block needs.
    var clock = new Mock<IClock>();

    var timer = new Timer();

    // Add the mocked dependencies to the blackboard.
    timer.Set(clock.Object);

    // Check that the initial state is the one we expect.
    timer.GetInitialState()
      .State
      .ShouldBeOfType<State.PoweredOff>();

    // Verify the timer has set its blackboard data correctly.
    timer.Has<Data>().ShouldBeTrue();
    timer.Get<IClock>().ShouldBe(clock.Object);
  }
}

public class PoweredOnStateTest()
{
  // We can't create abstract classes directly for testing, so we extend it.
  public record TestPoweredOnState : State.PoweredOn { }

  [Fact]
  public void TurnsOff()
  {
    var state = new TestPoweredOnState();

    state.CreateFakeContext();

    state.On(new Input.PowerButtonPressed()).State
      .ShouldBeOfType<State.PoweredOff>();
  }
}

public class IdleStateTest()
{
  [Fact]
  public void ChangesDuration()
  {
    var state = new State.PoweredOn.Idle();
    var context = state.CreateFakeContext();

    context.Set(new Data() { Duration = 30.0d });

    var duration = 45;

    state.On(new Input.ChangeDuration(duration))
      .State
      .ShouldBeOfType<State.PoweredOn.Idle>();

    context.Get<Data>().Duration.ShouldBe(duration);
  }

  [Fact]
  public void Starts()
  {
    var state = new State.PoweredOn.Idle();
    _ = state.CreateFakeContext();

    state.On(new Input.StartStopButtonPressed())
      .State
      .ShouldBeOfType<State.PoweredOn.Countdown>();
  }
}

public class CountdownStateTest
{
  [Fact]
  public void AddsTimeElapsedInputWhenClockFires()
  {
    var state = new State.PoweredOn.Countdown();
    var context = state.CreateFakeContext();

    var clock = new Mock<IClock>();
    context.Set(clock.Object);

    state.Attach(context);
    clock.Raise(c => c.TimeElapsed += null, 1.0d);

    // Make sure state unsubscribes.
    state.Detach();

    // Fire event again to make sure it doesn't get added.
    clock.Raise(c => c.TimeElapsed += null, 1.0d);

    context.Inputs.ShouldBe([new Input.TimeElapsed(1.0d)]);
  }

  [Fact]
  public void OnTimeElapsedStartsBeeping()
  {
    var state = new State.PoweredOn.Countdown();
    var context = state.CreateFakeContext();

    context.Set(new Data() { TimeRemaining = 1.0d });

    state.On(new Input.TimeElapsed(1.0d))
      .State
      .ShouldBeOfType<State.PoweredOn.Beeping>();
  }

  [Fact]
  public void OnTimeElapsedKeepsCountingDown()
  {
    var state = new State.PoweredOn.Countdown();
    var context = state.CreateFakeContext();

    context.Set(new Data() { TimeRemaining = 2.0d });

    state.On(new Input.TimeElapsed(1.0d))
      .State
      .ShouldBeOfType<State.PoweredOn.Countdown>();

    context.Get<Data>().TimeRemaining.ShouldBe(1.0d);
  }

  [Fact]
  public void OnStartStopButtonPressedIdles()
  {
    var state = new State.PoweredOn.Countdown();
    _ = state.CreateFakeContext();

    state.On(new Input.StartStopButtonPressed())
      .State
      .ShouldBeOfType<State.PoweredOn.Idle>();
  }
}

public class BeepingStateTest
{
  [Fact]
  public void PlaysBeepingSoundOnEnter()
  {
    var state = new State.PoweredOn.Beeping();
    var context = state.CreateFakeContext();

    state.Enter();

    context.Outputs.ShouldBe([new Output.PlayBeepingSound()]);
  }

  [Fact]
  public void StopsBeepingSoundOnExit()
  {
    var state = new State.PoweredOn.Beeping();
    var context = state.CreateFakeContext();

    state.Exit();

    context.Outputs.ShouldBe([new Output.StopBeepingSound()]);
  }

  [Fact]
  public void OnStartStopButtonPressedIdles()
  {
    var state = new State.PoweredOn.Beeping();
    _ = state.CreateFakeContext();

    state.On(new Input.StartStopButtonPressed())
      .State
      .ShouldBeOfType<State.PoweredOn.Idle>();
  }
}

public class PoweredOffStateTest()
{
  [Fact]
  public void TurnsOn()
  {
    var state = new State.PoweredOff();
    _ = state.CreateFakeContext();

    state.On(new Input.PowerButtonPressed())
      .State
      .ShouldBeOfType<State.PoweredOn.Idle>();
  }
}
