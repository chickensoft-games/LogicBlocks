namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitchAdvanced : LogicBlock<LightSwitchAdvanced.State> {
  public override State GetInitialState(IContext context) =>
    new State.Off(context);

  public static class Input {
    public readonly record struct Toggle;
  }

  public abstract record State(IContext Context) : StateLogic(Context) {
    public record On : State, IGet<Input.Toggle> {
      public On(IContext context) : base(context) {
        // Announce that we are now on.
        OnEnter<On>(
          (context) => Context.Output(new Output.StatusChanged(IsOn: true))
        );
      }

      // If we're toggled while we're on, we turn off.
      State IGet<Input.Toggle>.On(Input.Toggle input) => new Off(Context);
    }

    public record Off : State, IGet<Input.Toggle> {
      public Off(IContext context) : base(context) {
        // Announce that we are now off.
        OnEnter<On>(
          (context) => Context.Output(new Output.StatusChanged(IsOn: false))
        );
      }

      // If we're toggled while we're off, we turn on.
      State IGet<Input.Toggle>.On(Input.Toggle input) => new On(Context);
    }
  }

  public static class Output {
    public readonly record struct StatusChanged(bool IsOn);
  }
}
