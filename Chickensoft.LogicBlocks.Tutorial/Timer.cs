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

public interface ITimer : ILogicBlock;

[Meta]
public partial class Timer : LogicBlock, ITimer
{
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
    Set(new TimerState.PoweredOff());
    Set(new TimerState.PoweredOn.Idle());
    Set(new TimerState.PoweredOn.Countdown());
  }
}
