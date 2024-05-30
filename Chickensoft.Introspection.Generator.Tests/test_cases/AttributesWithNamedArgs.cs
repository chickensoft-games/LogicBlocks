namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Introspection.Generator.Tests.TestUtils;

[Meta, Id("attributes_with_named_args")]
public partial class AttributesWithNamedArgs {
  [Tag("name", Number = 10)]
  public required string Name { get; init; }
}
