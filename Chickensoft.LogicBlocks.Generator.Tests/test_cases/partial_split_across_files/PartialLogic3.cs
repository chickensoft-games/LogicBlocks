namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic :
  LogicBlock<PartialLogic.Input, PartialLogic.State, PartialLogic.Output> {
  public abstract partial record State : StateLogic {
    public partial record A : State, IGet<Input.One> {
      public State On(Input.One input) {
        Context.Output(new Output.OutputA());
        return new B(Context);
      }
    }
  }
}
