namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class LightSwitchLogic : LogicBlock
{
  public LightSwitchLogic()
  {
    // register all possible states
    Set(new LightSwitchState.PoweredOn());
    Set(new LightSwitchState.PoweredOff());
  }

  public bool StartCalled { get; private set; }
  public bool StopCalled { get; private set; }

  public override void OnStart()
  {
    StartCalled = true;
    base.OnStart();
  }

  public override void OnStop()
  {
    StopCalled = true;
    base.OnStop();
  }
}

[StateDiagram]
public abstract record LightSwitchState : LogicBlockState
{
  public static class Input
  {
    public readonly record struct TurnOn();

    public readonly record struct TurnOff();
  }

  public static class Output
  {
    public readonly record struct PlayToggleSound();
  }

  public sealed record PoweredOn : LightSwitchState, IGet<Input.TurnOff>
  {
    public PoweredOn()
    {
      this.OnEnter(() => Output(new Output.PlayToggleSound()));
    }

    public Type On(in Input.TurnOff input) => To<PoweredOff>();
  }

  public sealed record PoweredOff : LightSwitchState, IGet<Input.TurnOn>
  {
    public PoweredOff()
    {
      this.OnEnter(() => Output(new Output.PlayToggleSound()));
    }

    public Type On(in Input.TurnOn input) => To<PoweredOn>();
  }
}
