namespace Chickensoft.LogicBlocks.Generator.Tests;

using System.Linq;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks.Generator.Tests.TestCases;
using Shouldly;
using Xunit;

public class BaseTypeTest {
  [Fact]
  public void GetsInheritedProperties() {
    var props = Types.GetAllProperties(
      TypeRegistry.Instance, typeof(DerivedModel)
    );

    props
      .Select(p => p.Name)
      .ShouldBe(["Name", "Age"], ignoreOrder: true);
  }
}
