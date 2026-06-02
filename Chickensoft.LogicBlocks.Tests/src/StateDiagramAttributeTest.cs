namespace Chickensoft.LogicBlocks.Tests;

using Shouldly;

public class StateDiagramAttributeTest
{
  [Fact]
  public void DefaultConstructorPathIsNull()
  {
    var attr = new StateDiagramAttribute();

    attr.Path.ShouldBeNull();
  }

  [Fact]
  public void ConstructorWithPath()
  {
    var attr = new StateDiagramAttribute("diagrams/");

    attr.Path.ShouldBe("diagrams/");
  }
}
