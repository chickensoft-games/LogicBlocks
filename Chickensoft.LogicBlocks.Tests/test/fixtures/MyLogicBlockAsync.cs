namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

#pragma warning disable CS1998
[StateMachine]
public partial class MyLogicBlockAsync :
LogicBlockAsync<MyLogicBlockAsync.State> {
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
          async (previous) => context.Output(new Output.SomeOutput())
        );
        OnExit<SomeState>(
          async (previous) => context.Output(new Output.SomeOutput())
        );
      }

      public async Task<State> On(Input.SomeInput input) {
        await Task.CompletedTask;
        Context.Output(new Output.SomeOutput());
        return new SomeOtherState(Context);
      }
    }

    public record SomeOtherState(IContext Context) : State(Context),
      IGet<Input.SomeOtherInput> {
      public async Task<State> On(Input.SomeOtherInput input) {
        await Task.CompletedTask;
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
#pragma warning restore CS1998
