namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic : LogicBlock<PartialLogic.State> {
  public abstract partial record State : StateLogic {
    public partial record A : State, IGet<Input.One> {
      public A() {
        OnEnter<A>(
          (previous) => Context.Output(new Output.OutputEnterA())
        );
        OnExit<A>(
          (next) => Context.Output(new Output.OutputExitA())
        );
      }
    }

    public record B : State;
  }
}
