namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

public enum SecondaryState {
  Blooped,
  Bopped
}

[StateMachine]
public partial class TestMachine : LogicBlock<TestMachine.State> {
  public static class Input {
    public readonly record struct Activate(SecondaryState Secondary);
    public readonly record struct Deactivate();
  }

  public abstract record State(IContext Context) : StateLogic(Context),
    IGet<Input.Activate> {
    public State On(Input.Activate input) =>
      input.Secondary switch {
        SecondaryState.Blooped => new Activated.Blooped(Context),
        SecondaryState.Bopped => new Activated.Bopped(Context),
        _ => throw new ArgumentException("Unrecognized secondary state.")
      };

    public abstract record Activated : State, IGet<Input.Deactivate> {
      public Activated(IContext context) : base(context) {
        OnEnter<Activated>(
          (previous) => context.Output(new Output.Activated())
        );
        OnExit<Activated>(
          (next) => context.Output(new Output.ActivatedCleanUp())
        );
      }

      public State On(Input.Deactivate input) => new Deactivated(Context);

      public record Blooped : Activated {
        public Blooped(IContext context) : base(context) {
          OnEnter<Blooped>(
            (previous) => context.Output(new Output.Blooped())
          );
          OnExit<Blooped>(
            (next) => context.Output(new Output.BloopedCleanUp())
          );
        }
      }

      public record Bopped : Activated {
        public Bopped(IContext context) : base(context) {
          OnEnter<Bopped>(
            (previous) => context.Output(new Output.Bopped())
          );
          OnExit<Bopped>(
            (next) => context.Output(new Output.BoppedCleanUp())
          );
        }
      }
    }

    public record Deactivated : State {
      public Deactivated(IContext context) : base(context) {
        OnEnter<Deactivated>(
          (previous) => context.Output(new Output.Deactivated())
        );
        OnExit<Deactivated>(
          (next) => context.Output(new Output.DeactivatedCleanUp())
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

  public override State GetInitialState(IContext context) =>
    new State.Deactivated(context);
}
