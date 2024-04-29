namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using Chickensoft.Introspection;

/// <summary>
/// A service that announces the passage of time, roughly once per second.
/// </summary>
public interface IClock {
  /// <summary>
  /// Invoked about every second or so. Provides the time since the last
  /// invocation.
  /// </summary>
  event Action<double> TimeElapsed;
}

[Introspective("timer")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class Timer : LogicBlock<Timer.State> {
  public override Transition GetInitialState() => To<State.PoweredOff>();

  /// <summary>Blackboard data for our hierarchical state machine.</summary>
  public sealed record Data {
    /// <summary>Number of seconds the timer should countdown.</summary>
    public double Duration { get; set; }
    /// <summary>Number of seconds still remaining.</summary>
    public double TimeRemaining { get; set; }
  }

  public Timer(IClock clock) {
    // Set shared data for all states in the blackboard.
    Set(new Data() { Duration = 30.0d });

    // Make sure all states can access the clock.
    Set(clock);
  }

  public static class Input {
    public readonly record struct PowerButtonPressed;
    public readonly record struct StartStopButtonPressed;
    public readonly record struct ResetButtonPressed;
    /// <summary>Change the duration of the timer.</summary>
    /// <param name="Duration">Number of seconds to countdown.</param>
    public readonly record struct ChangeDuration(double Duration);
    /// <summary>Tells the timer that time has passed.</summary>
    /// <param name="Delta">Number of seconds that have passed.</param>
    public readonly record struct TimeElapsed(double Delta);
  }

  [Introspective("timer_state")]
  public abstract partial record State : StateLogic<State> {
    [Introspective("timer_state_powered_off")]
    public partial record PoweredOff : State, IGet<Input.PowerButtonPressed> {
      public Transition On(Input.PowerButtonPressed input) =>
        To<PoweredOn.Idle>();
    }

    [Introspective("timer_state_powered_on")]
    public abstract partial record PoweredOn : State, IGet<Input.PowerButtonPressed> {
      public Transition On(Input.PowerButtonPressed input) => To<PoweredOff>();

      [Introspective("timer_state_powered_on_idle")]
      public partial record Idle : PoweredOn, IGet<Input.StartStopButtonPressed> {
        public Transition On(Input.ChangeDuration input) {
          Get<Data>().Duration = input.Duration;
          return ToSelf();
        }

        public Transition On(Input.StartStopButtonPressed input) =>
          To<Running>();
      }

      [Introspective("timer_state_powered_on_running")]
      public partial record Running : PoweredOn,
          IGet<Input.TimeElapsed>, IGet<Input.StartStopButtonPressed> {
        public Running() {
          OnAttach(() => Get<IClock>().TimeElapsed += OnTimeElapsed);
          OnDetach(() => Get<IClock>().TimeElapsed -= OnTimeElapsed);
        }

        private void OnTimeElapsed(double delta) =>
          Input(new Input.TimeElapsed(delta));

        public Transition On(Input.TimeElapsed input) {
          var data = Get<Data>();
          data.TimeRemaining -= input.Delta;
          return data.TimeRemaining <= 0.0d ? To<Beeping>() : ToSelf();
        }

        public Transition On(Input.StartStopButtonPressed input) => To<Idle>();
      }

      [Introspective("timer_state_powered_on_beeping")]
      public partial record Beeping : PoweredOn {
        public Beeping() {
          this.OnEnter(() => Output(new Output.PlayBeepingSound()));
          this.OnExit(() => Output(new Output.StopBeepingSound()));
        }
      }
    }

    public static class Output {
      public readonly record struct PlayBeepingSound;
      public readonly record struct StopBeepingSound;
    }
  }
}
