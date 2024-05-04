namespace Chickensoft.Introspection.Generator.Tests;

using Chickensoft.LogicBlocks;
using Shouldly;
using Xunit;

public class LogicBlockIdentifierTest {
  [LogicBlock(typeof(State), Diagram = true)]
  public class TestLogicBlock : LogicBlock<TestLogicBlock.State> {
    public override Transition GetInitialState() => To<State>();

    public record State : StateLogic<State>;
  }

  [Fact]
  public void IdentifiesLogicBlock() {
    var descendants = Introspection.Types.Graph.GetDescendantSubtypes(typeof(LogicBlockBase));

    // Make sure our logic block is identified as a descendant of the base type
    // for all logic blocks.
    //
    // Identifying types this way allows us to make special type resolvers for
    // serializing and deserializing logic blocks specifically.
    descendants.ShouldContain(typeof(TestLogicBlock));
  }
}
