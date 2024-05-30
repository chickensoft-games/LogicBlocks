namespace Chickensoft.Introspection.Generator.Tests.TestCases;

[Meta]
public partial class OuterContainer {
  public partial class MidContainer {
    public partial class ZOtherContainer { }
    public partial class AInnerContainer {
      [Meta]
      public partial class ZMyModel { }

      [Meta]
      public partial class AOtherModel { }
    }
  }
}
