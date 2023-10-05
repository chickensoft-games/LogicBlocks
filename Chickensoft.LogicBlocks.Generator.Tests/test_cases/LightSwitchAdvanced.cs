namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class LightSwitchAdvanced :
LogicBlock<
  LightSwitchAdvanced.IInput, LightSwitchAdvanced.State, LightSwitchAdvanced.IOutput
> {
  public override State GetInitialState(IContext context) =>
    new State.Off(context);

  public interface IInput {
    public readonly record struct Toggle : IInput;
  }

  public abstract record State(IContext Context) : StateLogic(Context) {
    public record On : State, IGet<IInput.Toggle> {
      public On(IContext context) : base(context) {
        // Announce that we are now on.
        OnEnter<On>(
          (context) => Context.Output(new IOutput.StatusChanged(IsOn: true))
        );
      }

      // If we're toggled while we're on, we turn off.
      State IGet<IInput.Toggle>.On(IInput.Toggle input) => new Off(Context);
    }

    public record Off : State, IGet<IInput.Toggle> {
      public Off(IContext context) : base(context) {
        // Announce that we are now off.
        OnEnter<On>(
          (context) => Context.Output(new IOutput.StatusChanged(IsOn: false))
        );
      }

      // If we're toggled while we're off, we turn on.
      State IGet<IInput.Toggle>.On(IInput.Toggle input) => new On(Context);
    }
  }

  public interface IOutput {
    public readonly record struct StatusChanged(bool IsOn) : IOutput;
  }
}
