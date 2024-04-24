namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;

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

[LogicBlock(typeof(State), Diagram = true)]
public class Timer : LogicBlock<Timer.IState> {
  public override IState GetInitialState() => Get<State.IPoweredOff>();

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

    // Store preallocated state instances to prevent memory allocations.
    Set<State.IPoweredOff>(new State.PoweredOff());
    Set<State.PoweredOn.IIdle>(new State.PoweredOn.Idle());
    Set<State.PoweredOn.IRunning>(new State.PoweredOn.Running());
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

  public interface IState : IStateLogic<IState>;
  public abstract record State : StateLogic<IState>, IState {

    public interface IPoweredOff : IState;
    public record PoweredOff :
    State, IPoweredOff, IGet<Input.PowerButtonPressed> {
      public IState On(in Input.PowerButtonPressed input) =>
        Get<PoweredOn.IIdle>();
    }

    public interface IPoweredOn : IState;
    public abstract record PoweredOn :
    State, IPoweredOn, IGet<Input.PowerButtonPressed> {
      public IState On(in Input.PowerButtonPressed input) => Get<PoweredOff>();

      public interface IIdle : IPoweredOn;
      public record Idle :
      PoweredOn, IIdle, IGet<Input.StartStopButtonPressed> {
        public IState On(in Input.ChangeDuration input) {
          Get<Data>().Duration = input.Duration;
          return this;
        }

        public IState On(in Input.StartStopButtonPressed input) =>
          Get<Running>();
      }

      public interface IRunning : IPoweredOn;
      public record Running : PoweredOn, IRunning,
      IGet<Input.TimeElapsed>, IGet<Input.StartStopButtonPressed> {
        public Running() {
          OnAttach(() => Get<IClock>().TimeElapsed += OnTimeElapsed);
          OnDetach(() => Get<IClock>().TimeElapsed -= OnTimeElapsed);
        }

        private void OnTimeElapsed(double delta) =>
          Input(new Input.TimeElapsed(delta));

        public IState On(in Input.TimeElapsed input) {
          var data = Get<Data>();
          data.TimeRemaining -= input.Delta;
          if (data.TimeRemaining <= 0.0d) {
            return Get<Beeping>();
          }
          return this;
        }

        public IState On(in Input.StartStopButtonPressed input) => Get<Idle>();
      }

      public interface IBeeping : IPoweredOn;
      public record Beeping : PoweredOn {
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
