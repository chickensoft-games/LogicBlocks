namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic :
  LogicBlock<PartialLogic.Input, PartialLogic.State, PartialLogic.Output> {
  public abstract partial record State : StateLogic {
    public partial record A : State, IGet<Input.One> {
      public A(Context context) : base(context) {
        Context.OnEnter<A>(
          (previous) => Context.Output(new Output.OutputEnterA())
        );
        Context.OnExit<A>(
          (next) => Context.Output(new Output.OutputExitA())
        );
      }
    }

    public record B(Context Context) : State(Context);
  }
}
