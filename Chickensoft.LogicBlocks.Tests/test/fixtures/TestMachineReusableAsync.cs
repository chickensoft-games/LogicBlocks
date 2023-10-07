namespace Chickensoft.LogicBlocks.Tests.Fixtures;
using Chickensoft.LogicBlocks.Generator;

#pragma warning disable CS1998

[StateMachine]
public partial class TestMachineReusableAsync :
LogicBlockAsync<TestMachineReusableAsync.State> {
  public static class Input {
    public readonly record struct Activate(SecondaryState Secondary);
    public readonly record struct Deactivate;
  }

  public abstract record State(IContext Context) : StateLogic(Context),
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
      public Activated(IContext context) : base(context) {
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
        public Blooped(IContext context) : base(context) {
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
        public Bopped(IContext context) : base(context) {
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
      public Deactivated(IContext context) : base(context) {
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

  public TestMachineReusableAsync() {
    Set(new State.Activated.Blooped(Context));
    Set(new State.Activated.Bopped(Context));
    Set(new State.Deactivated(Context));
  }

  public override State GetInitialState(IContext context) =>
    context.Get<State.Deactivated>();
}

#pragma warning restore CS1998
