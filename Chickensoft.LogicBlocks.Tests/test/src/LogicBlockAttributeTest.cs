namespace Chickensoft.LogicBlocks.Tests;

using Shouldly;
using Xunit;

public class LogicBlockAttributeTest
{
  [Fact]
  public void Initializes()
  {
    var attr = new LogicBlockAttribute(typeof(LogicBlockAttributeTest));
    attr.StateType.ShouldBe(typeof(LogicBlockAttributeTest));
    attr.Diagram.ShouldBeFalse();
  }

  [Fact]
  public void InitializesWithDiagramFlag()
  {
    var attr = new LogicBlockAttribute(typeof(LogicBlockAttributeTest))
    {
      Diagram = true
    };
    attr.StateType.ShouldBe(typeof(LogicBlockAttributeTest));
    attr.Diagram.ShouldBeTrue();
  }
}
