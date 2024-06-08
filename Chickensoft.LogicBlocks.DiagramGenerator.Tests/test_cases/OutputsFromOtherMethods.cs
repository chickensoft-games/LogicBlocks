namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

[LogicBlock(typeof(State), Diagram = true)]
public class OutputsFromOtherMethods :
LogicBlock<OutputsFromOtherMethods.State> {
  public override Transition GetInitialState() => To<State>();

  public record State : StateLogic<State> {
    public void Method1() => Output(new Output.A());
  }

  public static class Output {
    public readonly record struct A();
  }
}
