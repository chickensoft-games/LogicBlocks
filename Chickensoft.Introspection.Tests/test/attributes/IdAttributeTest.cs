namespace Chickensoft.Introspection.Tests.Attributes;

using Shouldly;
using Xunit;

public class IdAttributeTest {
  [Fact]
  public void Initializes() {
    var id = new IdAttribute("id");

    id.Id.ShouldBe("id");
  }
}
