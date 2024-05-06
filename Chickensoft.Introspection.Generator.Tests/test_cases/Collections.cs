namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using System.Collections.Generic;
using Chickensoft.Serialization;

[Meta]
public partial class Collections {
  [Save("nested_collections")]
  public Dictionary<List<string>, List<List<int>>> NestedCollections { get; } = new();
}
