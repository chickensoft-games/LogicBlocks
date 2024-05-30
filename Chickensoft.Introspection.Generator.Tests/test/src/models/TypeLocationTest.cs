namespace Chickensoft.Introspection.Generator.Tests.Models;

using System.Collections.Immutable;
using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class TypeLocationTest {
  [Fact]
  public void Equality() {
    var location = new TypeLocation(
      ImmutableArray<string>.Empty, ImmutableArray<TypeReference>.Empty
    );

    location.Equals(null).ShouldBeFalse();
  }
}
