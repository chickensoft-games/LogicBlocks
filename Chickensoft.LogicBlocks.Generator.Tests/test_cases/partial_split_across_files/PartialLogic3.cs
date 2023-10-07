namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic : LogicBlock<PartialLogic.State> {
  public abstract partial record State : StateLogic {
    public partial record A : State, IGet<Input.One> {
      public State On(Input.One input) {
        Context.Output(new Output.OutputA());
        return new B(Context);
      }

      public void DoSomething() => Context.Output(new Output.OutputSomething());
    }
  }
}
