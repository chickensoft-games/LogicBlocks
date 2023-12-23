namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

[StateMachine]
public partial class TestMachineReusable :
LogicBlock<TestMachineReusable.State> {
  public static class Input {
    public readonly record struct Activate(SecondaryState Secondary);
    public readonly record struct Deactivate;
  }

  public abstract record State : StateLogic, IGet<Input.Activate> {
    public State On(Input.Activate input) =>
      input.Secondary switch {
        SecondaryState.Blooped => Context.Get<Activated.Blooped>(),
        SecondaryState.Bopped => Context.Get<Activated.Bopped>(),
        _ => throw new ArgumentException("Unrecognized secondary state.")
      };

    public abstract record Activated : State, IGet<Input.Deactivate> {
      public Activated() {
        OnEnter<Activated>(
          (previous) => Context.Output(new Output.Activated())
        );
        OnExit<Activated>(
          (next) => Context.Output(new Output.ActivatedCleanUp())
        );
      }

      public State On(Input.Deactivate input) => Context.Get<Deactivated>();

      public record Blooped : Activated {
        public Blooped() {
          OnEnter<Blooped>(
            (previous) => Context.Output(new Output.Blooped())
          );
          OnExit<Blooped>(
            (next) => Context.Output(new Output.BloopedCleanUp())
          );
        }
      }

      public record Bopped : Activated {
        public Bopped() {
          OnEnter<Bopped>(
            (previous) => Context.Output(new Output.Bopped())
          );
          OnExit<Bopped>(
            (next) => Context.Output(new Output.BoppedCleanUp())
          );
        }
      }
    }

    public record Deactivated : State {
      public Deactivated() {
        OnEnter<Deactivated>(
          (previous) => Context.Output(new Output.Deactivated())
        );
        OnExit<Deactivated>(
          (next) => Context.Output(new Output.DeactivatedCleanUp())
        );
      }
    }
  }

  public static class Output {
    public readonly record struct Activated;
    public readonly record struct ActivatedCleanUp;
    public readonly record struct Deactivated;
    public readonly record struct DeactivatedCleanUp;
    public readonly record struct Blooped;
    public readonly record struct BloopedCleanUp;
    public readonly record struct Bopped;
    public readonly record struct BoppedCleanUp;
  }

  public TestMachineReusable() {
    Set(new State.Activated.Blooped());
    Set(new State.Activated.Bopped());
    Set(new State.Deactivated());
  }

  public override State GetInitialState(IContext context) =>
    Context.Get<State.Deactivated>();
}
