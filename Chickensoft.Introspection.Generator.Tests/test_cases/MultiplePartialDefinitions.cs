namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Introspection.Generator.Tests.TestUtils;

[Meta, Id("multiple_partial_definitions")]
public partial class PartialModel {
  [Tag("name")]
  public required string Name { get; init; }
}


public partial class PartialModel {
  [Tag("age")]
  public required int Age { get; init; }
}
