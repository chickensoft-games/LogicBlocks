namespace Chickensoft.Introspection.Generator.Tests.Models;

using System;
using System.Collections.Immutable;
using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class TypeReferenceTest {
  [Fact]
  public void MergePartialDefinition() {
    var a = new TypeReference(
      SimpleName: "A",
      Construction: Construction.Class,
      IsPartial: true,
      TypeParameters: ImmutableArray<string>.Empty
    );

    var b = new TypeReference(
      SimpleName: "B",
      Construction: Construction.Class,
      IsPartial: false,
      TypeParameters: ImmutableArray<string>.Empty
    );


    a.MergePartialDefinition(b).IsPartial.ShouldBeTrue();
    b.MergePartialDefinition(a).IsPartial.ShouldBeTrue();
  }

  [Fact]
  public void GetConstructionCodeString() {
    var a = new TypeReference(
      SimpleName: "A",
      Construction: Construction.Struct,
      IsPartial: false,
      TypeParameters: ImmutableArray<string>.Empty
    );

    a.CodeString.ShouldBe("struct A");

    var b = a with {
      Construction = (Construction)(-1)
    };

    Should.Throw<ArgumentException>(() => b.CodeString);
  }
}
