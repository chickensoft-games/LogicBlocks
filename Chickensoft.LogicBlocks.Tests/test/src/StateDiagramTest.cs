namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Generator;
using Shouldly;
using Xunit;

public class StateDiagramTest {
  [Fact]
  public void Initializes() {
    var attr = new StateDiagram(typeof(StateDiagramTest));
    attr.StateType.ShouldBe(typeof(StateDiagramTest));
  }
}
