namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class Patterns : LogicBlock<Patterns.State> {
  public enum Mode { One, Two, Three }

  public override State GetInitialState() => new State.One();

  public static class Input {
    public readonly record struct SetMode(Mode Mode);
  }

  public abstract record State : StateLogic, IGet<Input.SetMode> {
    public State On(Input.SetMode input) => input.Mode switch {
      Mode.One => new One(),
      Mode.Two => new Two(),
      Mode.Three => true switch {
        true => new Three(),
        false => throw new NotImplementedException()
      },
      _ => throw new NotImplementedException()
    };
    public record One : State;
    public record Two : State;
    public record Three : State;
  }
}
