namespace Chickensoft.Introspection.Generator.Tests.Models;

using System.Collections.Immutable;
using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class DeclaredAttributeTest {
  [Fact]
  public void Equality() {
    var attr = new DeclaredAttribute(
      "", ImmutableArray<string>.Empty, ImmutableArray<string>.Empty
    );

    attr.GetHashCode().ShouldBeOfType<int>();

    attr.ShouldBe(
      new DeclaredAttribute(
        "", ImmutableArray<string>.Empty, ImmutableArray<string>.Empty
      )
    );

    new DeclaredAttribute(
      "", ImmutableArray<string>.Empty, new string[] { "b" }.ToImmutableArray()
    ).ShouldNotBe(
      new DeclaredAttribute(
        "a", ImmutableArray<string>.Empty, ImmutableArray<string>.Empty
      )
    );

    attr.ShouldNotBe(null);
  }
}
