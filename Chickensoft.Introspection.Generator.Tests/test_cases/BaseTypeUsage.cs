namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Meta]
public partial class BaseModel {
  [Save("name")]
  public string Name { get; set; } = "";
}

[Meta]
public partial class DerivedModel : BaseModel {
  [Save("age")]
  public int Age { get; set; }
}
