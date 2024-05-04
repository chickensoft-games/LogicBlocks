namespace Chickensoft.Introspection.Generator.Tests;

using Chickensoft.Introspection.Generator.Tests.TestCases;
using Shouldly;
using Xunit;

public class NameOfIdTest {
  [Fact]
  public void ModelWithNameOfIdWorks() {
    var metatype = TypeRegistry.Instance.Metatypes[typeof(NameOfId)];
    metatype.Id.ShouldBe(nameof(NameOfId));
  }
}
