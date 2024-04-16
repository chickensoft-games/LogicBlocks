namespace Chickensoft.LogicBlocks.Generator.Tests.Types.TestCases;

public partial class MyContainerClass {
  [LogicModel("my_model")]
  public partial record MyModel {
    [Save("name")]
    public string Name { get; set; } = "";

    [Save("age")]
    public int? Age { get; set; } = 0;
  }
}
