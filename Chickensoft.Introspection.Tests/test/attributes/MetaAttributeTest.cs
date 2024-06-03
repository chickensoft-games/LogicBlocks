namespace Chickensoft.Introspection.Tests.Attributes;

using Shouldly;
using Xunit;

public class MetaAttributeTest {
  [Fact]
  public void Initializes() {
    var attr = new MetaAttribute(typeof(MetaAttributeTest));

    attr.Mixins[0].ShouldBe(typeof(MetaAttributeTest));
  }
}
