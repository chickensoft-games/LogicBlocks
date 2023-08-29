namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

#pragma warning disable CS1998
[StateMachine]
public partial class MyLogicBlockAsync :
  LogicBlockAsync<
    MyLogicBlockAsync.Input, MyLogicBlockAsync.State, MyLogicBlockAsync.Output
  > {
  public override State GetInitialState(IContext context) =>
    new State.SomeState(context);

  public abstract record Input {
    public record SomeInput : Input;
    public record SomeOtherInput : Input;
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

  public abstract record Output {
    public record SomeOutput : Output;
    public record SomeOtherOutput : Output;
  }
}
#pragma warning restore CS1998
