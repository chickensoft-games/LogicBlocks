namespace Chickensoft.Introspection.Tests.Attributes;

using Shouldly;
using Xunit;

public class MixinAttributeTest {
  [Fact]
  public void Initializes() {
    var attr = new MixinAttribute();

    attr.ShouldBeOfType<MixinAttribute>();
  }
}
