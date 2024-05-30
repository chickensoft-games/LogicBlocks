namespace Chickensoft.Introspection.Generator.Tests.TestCases;

public partial class Outer {
  private sealed partial class Inner {
    [Meta]
    public sealed partial class NotVisibleFromGlobalScope { }
  }
}
