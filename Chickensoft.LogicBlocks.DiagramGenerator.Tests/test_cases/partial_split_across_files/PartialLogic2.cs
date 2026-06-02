namespace Chickensoft.LogicBlocks.Generator.Tests;

public partial class PartialLogic : LogicBlock
{
  public abstract partial record BaseState : LogicBlockState
  {
    public partial record A : BaseState, IGet<Input.One>
    {
      public A()
      {
        this.OnEnter(() => Output(new Output.OutputEnterA()));
        this.OnExit(() => Output(new Output.OutputExitA()));
      }
    }

    public record B : BaseState;
  }
}
