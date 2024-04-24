namespace Chickensoft.LogicBlocks.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Introspective("base_model")]
public partial class BaseModel {
  [Save("name")]
  public string Name { get; set; } = "";
}

[Introspective("derived_model")]
public partial class DerivedModel : BaseModel {
  [Save("age")]
  public int Age { get; set; }
}
