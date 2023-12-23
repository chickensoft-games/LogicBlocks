#pragma warning disable CS1998

namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

public interface IMyLogicBlockAsync : ILogicBlockAsync<
  MyLogicBlockAsync.IState
> { }

[StateMachine]
public partial class MyLogicBlockAsync : LogicBlockAsync<
  MyLogicBlockAsync.IState
>, IMyLogicBlockAsync {
  public override State GetInitialState() => new State.SomeState();

  public static class Input {
    public readonly record struct SomeInput;
    public readonly record struct SomeOtherInput;
  }

  public interface IState : IStateLogic { }

  public abstract record State : StateLogic, IState {
    public record SomeState : State, IGet<Input.SomeInput> {
      public SomeState() {
        OnEnter<SomeState>(
          async (previous) => Context.Output(new Output.SomeOutput())
        );
        OnExit<SomeState>(
          async (previous) => Context.Output(new Output.SomeOutput())
        );
      }

      public async Task<IState> On(Input.SomeInput input) {
        Context.Output(new Output.SomeOutput());
        return new SomeOtherState();
      }
    }

    public record SomeOtherState : State,
    IGet<Input.SomeOtherInput> {
      public async Task<IState> On(Input.SomeOtherInput input) {
        Context.Output(new Output.SomeOtherOutput());
        return new SomeState();
      }
    }
  }

  public static class Output {
    public readonly record struct SomeOutput;
    public readonly record struct SomeOtherOutput;
  }
}

#pragma warning restore
