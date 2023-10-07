namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

[StateMachine]
public partial class MyLogicBlock : LogicBlock<MyLogicBlock.State> {
  public override State GetInitialState(IContext context) =>
    new State.SomeState(context);

  public static class Input {
    public readonly record struct SomeInput;
    public readonly record struct SomeOtherInput;
  }

  public abstract record State(IContext Context) : StateLogic(Context) {
    public record SomeState : State, IGet<Input.SomeInput> {
      public SomeState(IContext context) : base(context) {
        OnEnter<SomeState>(
          (previous) => context.Output(new Output.SomeOutput())
        );
        OnExit<SomeState>(
          (previous) => context.Output(new Output.SomeOutput())
        );
      }

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

  public static class Output {
    public readonly record struct SomeOutput;
    public readonly record struct SomeOtherOutput;
  }
}
