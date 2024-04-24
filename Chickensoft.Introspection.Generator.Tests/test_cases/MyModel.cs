namespace Chickensoft.LogicBlocks.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Mixin]
public interface IMyMixin : IMixin<IMyMixin> {
  void IMixin<IMyMixin>.Handler() { }
}

public partial class MyContainerClass {
  [Introspective("my_model", typeof(IMyMixin))]
  public partial record MyModel {
    [Save("name")]
    public string Name { get; set; } = "";

    [Save("age")]
    public int? Age { get; set; } = 0;
  }
}
