namespace Chickensoft.LogicBlocks.Generator.Tests;

using System;

[LogicBlock(typeof(State), Diagram = true)]
public class Patterns : LogicBlock<Patterns.State> {
  public enum Mode { One, Two, Three }

  public override Transition GetInitialState() => To<State.One>();

  public static class Input {
    public readonly record struct SetMode(Mode Mode);
  }

  public abstract record State : StateLogic<State>, IGet<Input.SetMode> {
    public Transition On(Input.SetMode input) => input.Mode switch {
      Mode.One => To<One>(),
      Mode.Two => To<Two>(),
      Mode.Three => true switch {
        true => To<Three>(),
        false => throw new NotImplementedException()
      },
      _ => throw new NotImplementedException()
    };
    public record One : State;
    public record Two : State;
    public record Three : State;
  }
}
