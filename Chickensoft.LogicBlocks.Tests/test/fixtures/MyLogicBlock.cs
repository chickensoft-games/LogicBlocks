namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

public interface IMyLogicBlock : ILogicBlock<MyLogicBlock.State>;

[Meta("my_logic_block")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class MyLogicBlock : LogicBlock<MyLogicBlock.State>, IMyLogicBlock {
  public override Transition GetInitialState() => To<State.SomeState>();

  public static class Input {
    public readonly record struct SomeInput;
    public readonly record struct SomeOtherInput;
  }

  [Meta("my_logic_block_state")]
  public abstract partial record State : StateLogic<State> {
    public partial record SomeState : State, IGet<Input.SomeInput> {
      public SomeState() {
        this.OnEnter(() => Output(new Output.SomeOutput()));
        this.OnExit(() => Output(new Output.SomeOutput()));
      }

      public Transition On(in Input.SomeInput input) {
        Output(new Output.SomeOutput());
        return To<SomeOtherState>();
      }
    }

    [Meta("my_logic_block_state_some_other")]
    public partial record SomeOtherState : State, IGet<Input.SomeOtherInput> {
      public Transition On(in Input.SomeOtherInput input) {
        Output(new Output.SomeOtherOutput());
        return To<SomeState>();
      }
    }
  }

  public static class Output {
    public readonly record struct SomeOutput;
    public readonly record struct SomeOtherOutput;
  }
}
