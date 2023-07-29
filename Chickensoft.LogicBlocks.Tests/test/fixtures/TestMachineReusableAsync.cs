namespace Chickensoft.LogicBlocks.Tests.Fixtures;
using Chickensoft.LogicBlocks.Generator;

#pragma warning disable CS1998

[StateMachine]
public partial class TestMachineReusableAsync :
  LogicBlockAsync<
  TestMachineReusableAsync.Input,
  TestMachineReusableAsync.State,
  TestMachineReusableAsync.Output
> {
  public abstract record Input {
    public record Activate(SecondaryState Secondary) : Input;
    public record Deactivate() : Input;
  }

  public abstract record State(Context Context) : StateLogic(Context),
    IGet<Input.Activate> {
    public async Task<State> On(Input.Activate input) {
      await Task.Delay(5);

      return input.Secondary switch {
        SecondaryState.Blooped => Context.Get<Activated.Blooped>(),
        SecondaryState.Bopped => Context.Get<Activated.Bopped>(),
        _ => throw new ArgumentException("Unrecognized secondary state.")
      };
    }

    public abstract record Activated : State, IGet<Input.Deactivate> {
      public Activated(Context context) : base(context) {
        OnEnter<Activated>(
          async (previous) => {
            await Task.Delay(10);
            context.Output(new Output.Activated());
          }
        );
        OnExit<Activated>(
          async (next) => {
            await Task.Delay(20);
            context.Output(new Output.ActivatedCleanUp());
          }
        );
      }

      public async Task<State> On(Input.Deactivate input) =>
        Context.Get<Deactivated>();

      public record Blooped : Activated {
        public Blooped(Context context) : base(context) {
          OnEnter<Blooped>(
            async (previous) => {
              await Task.Delay(10);
              context.Output(new Output.Blooped());
            }
          );
          OnExit<Blooped>(
            async (next) => {
              await Task.Delay(15);
              context.Output(new Output.BloopedCleanUp());
            }
          );
        }
      }

      public record Bopped : Activated {
        public Bopped(Context context) : base(context) {
          OnEnter<Bopped>(
            async (previous) => {
              await Task.Delay(10);
              context.Output(new Output.Bopped());
            }
          );
          OnExit<Bopped>(
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
        OnEnter<Deactivated>(
          async (previous) => {
            await Task.Delay(20);
            context.Output(new Output.Deactivated());
          }
        );
        OnExit<Deactivated>(
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

  public TestMachineReusableAsync() {
    Set(new State.Activated.Blooped(Context));
    Set(new State.Activated.Bopped(Context));
    Set(new State.Deactivated(Context));
  }

  public override State GetInitialState(Context context) =>
    context.Get<State.Deactivated>();
}

#pragma warning restore CS1998
