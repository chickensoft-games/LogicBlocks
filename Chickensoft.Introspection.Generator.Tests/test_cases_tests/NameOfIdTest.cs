namespace Chickensoft.Introspection.Generator.Tests;

using Chickensoft.Introspection.Generator.Tests.TestCases;
using Shouldly;
using Xunit;

public class NameOfIdTest {
  [Fact]
  public void ModelWithNameOfIdWorks() {
    var idMetadata =
      TypeRegistry.Instance.VisibleTypes[typeof(NameOfId)]
        .ShouldBeAssignableTo<IIdentifiableTypeMetadata>().ShouldNotBeNull();
    idMetadata.Id.ShouldBe(nameof(NameOfId));
  }
}
