namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class NestedStartLogic
{
  [StateDiagram]
  public abstract partial record BaseState : LogicBlockState
  {
    public partial record Running : BaseState, IGet<Input.Toggle>
    {
      public Running()
      {
        this.OnEnter(() => Output(new Output.Toggled()));
      }

      public Type On(in Input.Toggle input) => To<Idle>();
    }

    public partial record Idle : BaseState, IGet<Input.Toggle>
    {
      public Type On(in Input.Toggle input) => To<Running>();
    }
  }
}
