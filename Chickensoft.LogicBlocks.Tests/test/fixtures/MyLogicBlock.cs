namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

public interface IMyLogicBlock : ILogicBlock<MyLogicBlock.IState> { }

[StateMachine]
public partial class MyLogicBlock : LogicBlock<MyLogicBlock.IState>, IMyLogicBlock {
  public override State GetInitialState(IContext context) =>
    new State.SomeState();

  public static class Input {
    public readonly record struct SomeInput;
    public readonly record struct SomeOtherInput;
  }

  public interface IState : IStateLogic { }

  public abstract record State : StateLogic, IState {
    public record SomeState : State, IGet<Input.SomeInput> {
      public SomeState() {
        OnEnter<SomeState>(
          (previous) => Context.Output(new Output.SomeOutput())
        );
        OnExit<SomeState>(
          (previous) => Context.Output(new Output.SomeOutput())
        );
      }

      public IState On(Input.SomeInput input) {
        Context.Output(new Output.SomeOutput());
        return new SomeOtherState();
      }
    }

    public record SomeOtherState : State,
      IGet<Input.SomeOtherInput> {
      public IState On(Input.SomeOtherInput input) {
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
