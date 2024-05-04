namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Mixin]
public interface IMyMixin : IMixin<IMyMixin> {
  void IMixin<IMyMixin>.Handler() { }
}

[Mixin]
public interface IMySecondMixin : IMixin<IMySecondMixin> {
  void IMixin<IMySecondMixin>.Handler() { }
}

public partial class MyContainerClass {
  [Meta("my_model", typeof(IMyMixin), typeof(IMySecondMixin))]
  public partial record MyModel {
    [Save("name")]
    public string Name { get; set; } = "";

    [Save("age")]
    public int? Age { get; set; } = 0;
  }
}
