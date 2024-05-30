namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.Introspection.Generator.Tests.TestUtils;

[Mixin]
public interface IMyMixin : IMixin<IMyMixin> {
  void IMixin<IMyMixin>.Handler() { }
}

[Mixin]
public interface IMySecondMixin : IMixin<IMySecondMixin> {
  void IMixin<IMySecondMixin>.Handler() { }
}

public partial class MyContainerClass {
  [Id("my_model"), Meta(typeof(IMyMixin), typeof(IMySecondMixin))]
  public partial record MyModel {
    [Tag("name")]
    public string Name { get; set; } = "";

    [Tag("age")]
    public int? Age { get; set; } = 0;
  }
}
