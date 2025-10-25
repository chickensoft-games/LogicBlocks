namespace Chickensoft.LogicBlocks.Tutorial;

using System;
using Chickensoft.Introspection;

/// <summary>
/// A service that announces the passage of time, roughly once per second.
/// </summary>
public interface IClock
{
  /// <summary>
  /// Invoked about every second or so. Provides the time since the last
  /// invocation.
  /// </summary>
  event Action<double> TimeElapsed;
}

public interface ITimer : ILogicBlock<Timer.State>;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class Timer : LogicBlock<Timer.State>, ITimer
{
  public override Transition GetInitialState() => To<State.PoweredOff>();

  /// <summary>Blackboard data for our hierarchical state machine.</summary>
  public sealed record Data
  {
    /// <summary>Number of seconds the timer should countdown.</summary>
    public double Duration { get; set; }
    /// <summary>Number of seconds still remaining.</summary>
    public double TimeRemaining { get; set; }
  }

  public Timer()
  {
    // Set shared data for all states in the blackboard.
    Set(new Data() { Duration = 30.0d });
  }

  public static class Input
  {
    public readonly record struct PowerButtonPressed;
    /// <summary>Change the duration of the timer.</summary>
    /// <param name="Duration">Number of seconds to countdown.</param>
    public readonly record struct ChangeDuration(double Duration);
    public readonly record struct StartStopButtonPressed;
    /// <summary>Tells the timer that time has passed.</summary>
    /// <param name="Delta">Number of seconds that have passed.</param>
    public readonly record struct TimeElapsed(double Delta);
  }

  public static class Output
  {
    public readonly record struct PlayBeepingSound;
    public readonly record struct StopBeepingSound;
  }

  public abstract record State : StateLogic<State>
  {
    public record PoweredOff : State, IGet<Input.PowerButtonPressed>
    {
      public Transition On(in Input.PowerButtonPressed input) =>
        To<PoweredOn.Idle>();
    }

    public abstract record PoweredOn : State, IGet<Input.PowerButtonPressed>
    {
      public Transition On(in Input.PowerButtonPressed input) => To<PoweredOff>();

      public record Idle : PoweredOn, IGet<Input.StartStopButtonPressed>, IGet<Input.ChangeDuration>
      {
        public Transition On(in Input.ChangeDuration input)
        {
          Get<Data>().Duration = input.Duration;
          return ToSelf();
        }

        public Transition On(in Input.StartStopButtonPressed input) =>
          To<Countdown>();
      }

      public record Countdown : PoweredOn,
          IGet<Input.TimeElapsed>, IGet<Input.StartStopButtonPressed>
      {
        public Countdown()
        {
          OnAttach(() => Get<IClock>().TimeElapsed += OnTimeElapsed);
          OnDetach(() => Get<IClock>().TimeElapsed -= OnTimeElapsed);
        }

        private void OnTimeElapsed(double delta) =>
          Input(new Input.TimeElapsed(delta));

        public Transition On(in Input.TimeElapsed input)
        {
          var data = Get<Data>();
          data.TimeRemaining -= input.Delta;
          return data.TimeRemaining <= 0.0d ? To<Beeping>() : ToSelf();
        }

        public Transition On(in Input.StartStopButtonPressed input) => To<Idle>();
      }

      public record Beeping : PoweredOn, IGet<Input.StartStopButtonPressed>
      {
        public Beeping()
        {
          this.OnEnter(() => Output(new Output.PlayBeepingSound()));
          this.OnExit(() => Output(new Output.StopBeepingSound()));
        }

        public Transition On(in Input.StartStopButtonPressed input) =>
          To<Idle>();
      }
    }
  }
}
