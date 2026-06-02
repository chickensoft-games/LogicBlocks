namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic : LogicBlock
{
  public abstract partial record BaseState : LogicBlockState
  {
    public partial record A : BaseState, IGet<Input.One>
    {
      public Type On(in Input.One input)
      {
        Output(new Output.OutputA());
        return To<B>();
      }

      public void DoSomething() => Output(new Output.OutputSomething());
    }
  }
}
