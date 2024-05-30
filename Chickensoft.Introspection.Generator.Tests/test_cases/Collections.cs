namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using System.Collections.Generic;
using Chickensoft.Introspection.Generator.Tests.TestUtils;

[Meta]
public partial class Collections {
  [Tag("nested_collections")]
  public Dictionary<List<string>, List<List<int>>> NestedCollections { get; } = new();
}
