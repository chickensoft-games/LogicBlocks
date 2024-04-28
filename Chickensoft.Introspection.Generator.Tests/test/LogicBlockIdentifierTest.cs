namespace Chickensoft.LogicBlocks.Generator.Tests;

using Chickensoft.Introspection;
using Shouldly;
using Xunit;

public class LogicBlockIdentifierTest {
  [LogicBlock(typeof(TestState), Diagram = true)]
  public class TestLogicBlock : LogicBlock<TestLogicBlock.TestState> {
    public override TestState GetInitialState() => new();

    public record TestState : StateLogic<TestState>;
  }

  [Fact]
  public void IdentifiesLogicBlock() {
    var descendants = Types.Graph.GetDescendantSubtypes(typeof(LogicBlockBase));

    // Make sure our logic block is identified as a descendant of the base type
    // for all logic blocks.
    //
    // Identifying types this way allows us to make special type resolvers for
    // serializing and deserializing logic blocks specifically.
    descendants.ShouldContain(typeof(TestLogicBlock));
  }
}
