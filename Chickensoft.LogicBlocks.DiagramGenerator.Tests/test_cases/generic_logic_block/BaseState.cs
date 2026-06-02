namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class MyGenericType<T>
{
  public partial class GenericLogicBlock
  {
    public static class Input
    {
      public readonly record struct InputOne;

      public readonly record struct InputTwo;
    }

    [StateDiagram]
    public abstract record BaseState : LogicBlockState;

    public record StateOne : BaseState, IGet<Input.InputOne>
    {
      public Type On(in Input.InputOne input) => To<StateTwo>();
    }

    public record StateTwo : BaseState, IGet<Input.InputTwo>
    {
      public Type On(in Input.InputTwo input) => To<StateOne>();
    }
  }
}
