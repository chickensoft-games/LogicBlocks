namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Meta, Id("base_model")]
public partial class BaseModel {
  [Save("name")]
  public string Name { get; set; } = "";
}

[Meta, Id("derived_model")]
public partial class DerivedModel : BaseModel {
  [Save("age")]
  public int Age { get; set; }
}
