namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

[StateMachine]
public partial class TestMachineReusable :
  LogicBlock<
    TestMachineReusable.Input,
    TestMachineReusable.State,
    TestMachineReusable.Output
  > {
  public abstract record Input {
    public record Activate(SecondaryState Secondary) : Input;
    public record Deactivate() : Input;
  }

  public abstract record State(IContext Context) : StateLogic(Context),
    IGet<Input.Activate> {
    public State On(Input.Activate input) =>
      input.Secondary switch {
        SecondaryState.Blooped => Context.Get<Activated.Blooped>(),
        SecondaryState.Bopped => Context.Get<Activated.Bopped>(),
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

      public State On(Input.Deactivate input) => Context.Get<Deactivated>();

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

  public abstract record Output {
    public record Activated() : Output;
    public record ActivatedCleanUp() : Output;
    public record Deactivated() : Output;
    public record DeactivatedCleanUp() : Output;
    public record Blooped() : Output;
    public record BloopedCleanUp() : Output;
    public record Bopped() : Output;
    public record BoppedCleanUp() : Output;
  }

  public TestMachineReusable() {
    Set(new State.Activated.Blooped(Context));
    Set(new State.Activated.Bopped(Context));
    Set(new State.Deactivated(Context));
  }

  public override State GetInitialState(IContext context) =>
    context.Get<State.Deactivated>();
}
