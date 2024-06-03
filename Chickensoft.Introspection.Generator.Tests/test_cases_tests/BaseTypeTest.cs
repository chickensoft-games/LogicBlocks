namespace Chickensoft.Introspection.Generator.Tests;

using System.Linq;
using Chickensoft.Introspection.Generator.Tests.TestCases;
using Shouldly;
using Xunit;

public class BaseTypeTest {
  [Fact]
  public void GetsInheritedProperties() {
    var props = Types.Graph.GetProperties(typeof(DerivedModel));

    props
      .Select(p => p.Name)
      .ShouldBe(["Name", "Age"], ignoreOrder: true);
  }
}
