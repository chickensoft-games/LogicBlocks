namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic : LogicBlock<PartialLogic.State> {
  public abstract partial record State : StateLogic<State> {
    public partial record A : State, IGet<Input.One> {
      public A() {
        this.OnEnter(() => Context.Output(new Output.OutputEnterA()));
        this.OnExit(() => Context.Output(new Output.OutputExitA()));
      }
    }

    public record B : State;
  }
}
