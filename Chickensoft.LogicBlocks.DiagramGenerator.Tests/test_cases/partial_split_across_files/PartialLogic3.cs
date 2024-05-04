namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic : LogicBlock<PartialLogic.State> {
  public abstract partial record State : StateLogic<State> {
    public partial record A : State, IGet<Input.One> {
      public Transition On(in Input.One input) {
        Output(new Output.OutputA());
        return To<B>();
      }

      public void DoSomething() => Output(new Output.OutputSomething());
    }
  }
}
