namespace Chickensoft.LogicBlocks.Tests.Examples.Misc;

using Chickensoft.Introspection;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class LightSwitch : LogicBlock<LightSwitch.State> {
  public override Transition GetInitialState() => To<State.PoweredOff>();

  public static class Input {
    public readonly record struct Toggle;
  }

  public static class Output {
    public readonly record struct StatusChanged(bool IsOn);
  }

  public abstract record State : StateLogic<State> {
    public record PoweredOn : State, IGet<Input.Toggle> {
      public Transition On(in Input.Toggle input) => To<PoweredOff>();
    }

    public record PoweredOff : State, IGet<Input.Toggle> {
      public Transition On(in Input.Toggle input) => To<PoweredOn>();
    }
  }
}
