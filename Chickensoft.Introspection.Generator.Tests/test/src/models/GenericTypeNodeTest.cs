namespace Chickensoft.Introspection.Generator.Tests.Models;

using System.Collections.Immutable;
using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class GenericTypeNodeTest {
  [Fact]
  public void Equality() {
    var node = new GenericTypeNode(
      "Type", ImmutableArray<GenericTypeNode>.Empty
    );

    node.Equals(null).ShouldBeFalse();
  }
}
