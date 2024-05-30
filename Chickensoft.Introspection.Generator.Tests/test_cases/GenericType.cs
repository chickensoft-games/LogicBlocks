namespace Chickensoft.Introspection.Generator.Tests.TestCases;

public partial class Outer {
  public sealed partial class Inner<T> {
    [Meta]
    public sealed partial class NotVisibleFromGlobalScope { }
  }
}
