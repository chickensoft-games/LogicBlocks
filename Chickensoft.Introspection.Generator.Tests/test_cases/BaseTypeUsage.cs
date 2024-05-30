namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Introspection.Generator.Tests.TestUtils;

[Meta]
public partial class BaseModel {
  [Tag("name")]
  public string Name { get; set; } = "";
}

[Meta]
public partial class DerivedModel : BaseModel {
  [Tag("age")]
  public int Age { get; set; }
}
