namespace Chickensoft.LogicBlocks.Tutorial;

using System;

public abstract partial record TimerState : LogicBlockState
{
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

  public partial record PoweredOff : TimerState, IGet<Input.PowerButtonPressed>
  {
    public Type On(in Input.PowerButtonPressed input) =>
      To<PoweredOn.Idle>();
  }

  public abstract partial record PoweredOn : TimerState, IGet<Input.PowerButtonPressed>
  {
    public Type On(in Input.PowerButtonPressed input) => To<PoweredOff>();

    public partial record Idle : PoweredOn, IGet<Input.StartStopButtonPressed>, IGet<Input.ChangeDuration>
    {
      public Type On(in Input.ChangeDuration input)
      {
        Get<Timer.Data>().Duration = input.Duration;
        return ToSelf();
      }

      public Type On(in Input.StartStopButtonPressed input) =>
        To<Countdown>();
    }

    public partial record Countdown : PoweredOn,
      IGet<Input.TimeElapsed>, IGet<Input.StartStopButtonPressed>
    {
      public Countdown()
      {
        this.OnEnter(() => Get<IClock>().TimeElapsed += OnTimeElapsed);
        this.OnExit(() => Get<IClock>().TimeElapsed -= OnTimeElapsed);
      }

      private void OnTimeElapsed(double delta) =>
        Input(new Input.TimeElapsed(delta));

      public Type On(in Input.TimeElapsed input)
      {
        var data = Get<Timer.Data>();
        data.TimeRemaining -= input.Delta;
        return data.TimeRemaining <= 0.0d ? To<Beeping>() : ToSelf();
      }

      public Type On(in Input.StartStopButtonPressed input) => To<Idle>();
    }

    public partial record Beeping : PoweredOn, IGet<Input.StartStopButtonPressed>
    {
      public Beeping()
      {
        this.OnEnter(() => Output(new Output.PlayBeepingSound()));
        this.OnExit(() => Output(new Output.StopBeepingSound()));
      }

      public Type On(in Input.StartStopButtonPressed input) =>
        To<Idle>();
    }
  }
}
