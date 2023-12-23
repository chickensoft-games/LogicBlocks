namespace Chickensoft.LogicBlocks.Tests.Fixtures;

#pragma warning disable CS1998

public partial class TestMachineAsync :
LogicBlockAsync<TestMachineAsync.State> {
  public static class Input {
    public readonly record struct Activate(SecondaryState Secondary);
    public readonly record struct Deactivate();
  }

  public abstract record State : StateLogic, IGet<Input.Activate> {
    public async Task<State> On(Input.Activate input) {
      await Task.Delay(5);

      return input.Secondary switch {
        SecondaryState.Blooped => new Activated.Blooped(),
        SecondaryState.Bopped => new Activated.Bopped(),
        _ => throw new ArgumentException("Unrecognized secondary state.")
      };
    }

    public abstract record Activated : State, IGet<Input.Deactivate> {
      public Activated() {
        OnEnter<Activated>(
          async (previous) => {
            await Task.Delay(10);
            Context.Output(new Output.Activated());
          }
        );
        OnExit<Activated>(
          async (next) => {
            await Task.Delay(20);
            Context.Output(new Output.ActivatedCleanUp());
          }
        );
      }

      public async Task<State> On(Input.Deactivate input) => new Deactivated();

      public record Blooped : Activated {
        public Blooped() {
          OnEnter<Blooped>(
            async (previous) => {
              await Task.Delay(10);
              Context.Output(new Output.Blooped());
            }
          );
          OnExit<Blooped>(
            async (next) => {
              await Task.Delay(15);
              Context.Output(new Output.BloopedCleanUp());
            }
          );
        }
      }

      public record Bopped : Activated {
        public Bopped() {
          OnEnter<Bopped>(
            async (previous) => {
              await Task.Delay(10);
              Context.Output(new Output.Bopped());
            }
          );
          OnExit<Bopped>(
            async (next) => {
              await Task.Delay(20);
              Context.Output(new Output.BoppedCleanUp());
            }
          );
        }
      }
    }

    public record Deactivated : State {
      public Deactivated() {
        OnEnter<Deactivated>(
          async (previous) => {
            await Task.Delay(20);
            Context.Output(new Output.Deactivated());
          }
        );
        OnExit<Deactivated>(
          async (next) => {
            await Task.Delay(20);
            Context.Output(new Output.DeactivatedCleanUp());
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

  public override State GetInitialState() => new State.Deactivated();
}

#pragma warning restore CS1998
