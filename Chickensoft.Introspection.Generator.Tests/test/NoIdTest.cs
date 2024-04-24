namespace Chickensoft.LogicBlocks.Generator.Tests;

using Chickensoft.LogicBlocks.Generator.Tests.TestCases;
using Shouldly;
using Xunit;

public class NoIdTest {
  [Fact]
  public void ModelWithoutExplicitIdIsAllowed() {
    var metatype = TypeRegistry.Instance.Metatypes[typeof(ModelWithoutId)];
    metatype.Id.ShouldBe(nameof(ModelWithoutId));
  }
}
