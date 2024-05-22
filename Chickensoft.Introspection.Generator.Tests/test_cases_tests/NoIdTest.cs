namespace Chickensoft.Introspection.Generator.Tests;

using Chickensoft.Introspection.Generator.Tests.TestCases;
using Shouldly;
using Xunit;

public class NoIdTest {
  [Fact]
  public void ModelWithoutExplicitIdIsAllowed() {
    var metadata = TypeRegistry.Instance.VisibleTypes[typeof(NoId)]
      .ShouldBeOfType<IntrospectiveTypeMetadata>();
  }
}
