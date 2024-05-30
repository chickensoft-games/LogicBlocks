namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Introspection.Generator.Tests.TestUtils;

[Meta, Id("init_args_model")]
public partial class InitArgsModel {
  [Tag("name")]
  public required string Name { get; init; }

  [Tag("age")]
  public required int Age { get; init; }

  [Tag("description")]
  public string? Description { get; init; }

  [Tag("address")]
  public string? Address { get; set; } // not init
}
