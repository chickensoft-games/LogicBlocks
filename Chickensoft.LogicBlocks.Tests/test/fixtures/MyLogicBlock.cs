namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

[StateMachine]
public partial class MyLogicBlock :
  LogicBlock<MyLogicBlock.Input, MyLogicBlock.State, MyLogicBlock.Output> {
  public override State GetInitialState(IContext context) =>
    new State.SomeState(context);

  public abstract record Input {
    public record SomeInput : Input;
    public record SomeOtherInput : Input;
  }

  public abstract record State(IContext Context) : StateLogic(Context) {
    public record SomeState(IContext Context) : State(Context),
      IGet<Input.SomeInput> {
      public State On(Input.SomeInput input) {
        Context.Output(new Output.SomeOutput());
        return new SomeOtherState(Context);
      }
    }

    public record SomeOtherState(IContext Context) : State(Context),
      IGet<Input.SomeOtherInput> {
      public State On(Input.SomeOtherInput input) {
        Context.Output(new Output.SomeOtherOutput());
        return new SomeState(Context);
      }
    }
  }

  public abstract record Output {
    public record SomeOutput : Output;
    public record SomeOtherOutput : Output;
  }
}
