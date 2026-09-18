namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class SharedBaseStateLogic
{
  public partial record SharedState
  {
    public partial record Lit : SharedState, IGet<Input.Toggle>
    {
      public Lit()
      {
        this.OnEnter(() => Output(new Output.Toggled()));
      }

      public Type On(in Input.Toggle input) => To<Dark>();
    }

    public partial record Dark : SharedState, IGet<Input.Toggle>
    {
      public Type On(in Input.Toggle input) => To<Lit>();
    }
  }
}
