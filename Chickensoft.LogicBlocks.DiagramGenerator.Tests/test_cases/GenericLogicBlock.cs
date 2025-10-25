namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

using Chickensoft.LogicBlocks;

public class MyGenericType<T>
{
  [LogicBlock(typeof(MyGenericType<>.GenericLogicBlock.State), Diagram = true)]
  public class GenericLogicBlock : LogicBlock<GenericLogicBlock.State>
  {

    public static class Input
    {
      public readonly record struct InputOne;
      public readonly record struct InputTwo;
    }

    public abstract record State : StateLogic<State> { }
    public record StateOne : State, IGet<Input.InputOne>
    {
      public Transition On(in Input.InputOne input) => To<StateTwo>();
    }

    public record StateTwo : State, IGet<Input.InputTwo>
    {
      public Transition On(in Input.InputTwo input) => To<StateOne>();
    }

    public override Transition GetInitialState() => To<State>();
  }
}
