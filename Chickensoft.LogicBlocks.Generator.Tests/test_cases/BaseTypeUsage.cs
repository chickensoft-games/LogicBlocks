namespace Chickensoft.LogicBlocks.Generator.Tests.Types.TestCases;

[LogicModel("base_model")]
public partial class BaseModel {
  [Save("name")]
  public string Name { get; set; } = "";
}

[LogicModel("derived_model")]
public partial class DerivedModel : BaseModel {
  [Save("age")]
  public int Age { get; set; }
}
