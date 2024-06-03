namespace Chickensoft.Introspection.Tests.Attributes;

using Shouldly;
using Xunit;

public class VersionAttributeTest {
  [Fact]
  public void Initializes() {
    var attr = new VersionAttribute(2);

    attr.Version.ShouldBe(2);
  }
}
