namespace Chickensoft.LogicBlocks.Generator.Tests;

using System.Linq;
using Chickensoft.LogicBlocks.Generator.Tests.Types.TestCases;
using Shouldly;
using Xunit;

public class BaseTypeTest {
  [Fact]
  public void GetsInheritedProperties() {
    var registry = new TypeRegistry();

    var props = LogicBlockTypes.GetProperties(registry, typeof(DerivedModel));

    props
      .Select(p => p.Name)
      .ShouldBe(["Name", "Age"], ignoreOrder: true);
  }
}
