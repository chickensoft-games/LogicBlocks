namespace Chickensoft.LogicBlocks.Tests.Fixtures;

#pragma warning disable CS1998

public partial class TestMachineAsync :
  LogicBlockAsync<TestMachineAsync.Input, TestMachineAsync.State, TestMachineAsync.Output> {
  public abstract record Input {
    public record Activate(SecondaryState Secondary) : Input;
    public record Deactivate() : Input;
  }

  public abstract record State(Context Context) : StateLogic(Context),
    IGet<Input.Activate> {
    public async Task<State> On(Input.Activate input) {
      await Task.Delay(5);

      return input.Secondary switch {
        SecondaryState.Blooped => new Activated.Blooped(Context),
        SecondaryState.Bopped => new Activated.Bopped(Context),
        _ => throw new ArgumentException("Unrecognized secondary state.")
      };
    }

    public abstract record Activated : State, IGet<Input.Deactivate> {
      public Activated(Context context) : base(context) {
        context.OnEnter<Activated>(
          async (previous) => {
            await Task.Delay(10);
            context.Output(new Output.Activated());
          }
        );
        context.OnExit<Activated>(
          async (next) => {
            await Task.Delay(20);
            context.Output(new Output.ActivatedCleanUp());
          }
        );
      }

      public async Task<State> On(Input.Deactivate input) =>
        new Deactivated(Context);

      public record Blooped : Activated {
        public Blooped(Context context) : base(context) {
          context.OnEnter<Blooped>(
            async (previous) => {
              await Task.Delay(10);
              context.Output(new Output.Blooped());
            }
          );
          context.OnExit<Blooped>(
            async (next) => {
              await Task.Delay(15);
              context.Output(new Output.BloopedCleanUp());
            }
          );
        }
      }

      public record Bopped : Activated {
        public Bopped(Context context) : base(context) {
          context.OnEnter<Bopped>(
            async (previous) => {
              await Task.Delay(10);
              context.Output(new Output.Bopped());
            }
          );
          context.OnExit<Bopped>(
            async (next) => {
              await Task.Delay(20);
              context.Output(new Output.BoppedCleanUp());
            }
          );
        }
      }
    }

    public record Deactivated : State {
      public Deactivated(Context context) : base(context) {
        context.OnEnter<Deactivated>(
          async (previous) => {
            await Task.Delay(20);
            context.Output(new Output.Deactivated());
          }
        );
        context.OnExit<Deactivated>(
          async (next) => {
            await Task.Delay(20);
            context.Output(new Output.DeactivatedCleanUp());
          }
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

  public override State GetInitialState(Context context) =>
    new State.Deactivated(context);
}

#pragma warning restore CS1998
