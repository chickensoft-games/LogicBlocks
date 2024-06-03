namespace Chickensoft.Introspection.Generator.Tests.Models;

using System.Collections.Immutable;
using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class DeclaredTypeTest {
  private readonly DeclaredType _type = new(
  Reference: new TypeReference(
    "SomeType",
    Construction: Construction.Class,
    IsPartial: true,
    TypeParameters: ImmutableArray<string>.Empty
  ),
  SyntaxLocation: Microsoft.CodeAnalysis.Location.None,
  Location: new TypeLocation(
    Namespaces: ImmutableArray<string>.Empty,
    ContainingTypes: ImmutableArray<TypeReference>.Empty
  ),
  BaseType: null,
  Usings: ImmutableHashSet<UsingDirective>.Empty,
  Kind: DeclaredTypeKind.ConcreteType,
  IsStatic: false,
  IsPublicOrInternal: true,
  Properties: ImmutableArray<DeclaredProperty>.Empty,
  Attributes: ImmutableArray<DeclaredAttribute>.Empty,
  Mixins: ImmutableArray<string>.Empty
);

  [Fact]
  public void Version() {
    var type = _type with {
      Attributes = ImmutableArray.Create(
        new DeclaredAttribute(
          Name: Constants.VERSION_ATTRIBUTE_NAME,
          ConstructorArgs:
          new[] { "a" }.ToImmutableArray(),
          InitializerArgs: new[] { "b" }.ToImmutableArray()
        )
      )
    };

    type.Version.ShouldBe(1);
  }

  [Fact]
  public void MergePartialPicksCorrectSyntaxLocation() {
    var type = _type with {
      SyntaxLocation = Microsoft.CodeAnalysis.Location.None,
      Attributes = ImmutableArray.Create(
        new DeclaredAttribute(
          Name: Constants.INTROSPECTIVE_ATTRIBUTE_NAME,
          ConstructorArgs: ImmutableArray<string>.Empty,
          InitializerArgs: ImmutableArray<string>.Empty
        )
      )
    };

    var other = _type with {
      SyntaxLocation = default!
    };

    var merged = type.MergePartialDefinition(other);

    merged.SyntaxLocation.ShouldBe(type.SyntaxLocation);

    var inverse = other.MergePartialDefinition(type);

    inverse.SyntaxLocation.ShouldBe(type.SyntaxLocation);
  }

  [Fact]
  public void GetState() {
    var @interface = _type with {
      Kind = DeclaredTypeKind.Interface
    };

    // Not a supported definition type
    @interface
      .GetState(true)
      .ShouldBe(DeclaredType.DeclaredTypeState.Unsupported);

    // Not visible
    _type
      .GetState(false)
      .ShouldBe(DeclaredType.DeclaredTypeState.Unsupported);

    var invalid = _type with {
      Kind = (DeclaredTypeKind)(-1)
    };

    // All other conditions fall back to just "type"
    invalid
      .GetState(true)
      .ShouldBe(DeclaredType.DeclaredTypeState.Type);
  }

  [Fact]
  public void DoesNotWriteMetadataForUnsupportedStates() {
    var @interface = _type with {
      Kind = DeclaredTypeKind.Interface
    };

    var writer = TypeGenerator.CreateCodeWriter();

    @interface.WriteMetadata(writer, true);

    writer.InnerWriter.ToString().ShouldBeEmpty();
  }

  [Fact]
  public void HasFallbackId() {
    var type = _type with {
      SyntaxLocation = Microsoft.CodeAnalysis.Location.None,
      Attributes = ImmutableArray.Create(
        new DeclaredAttribute(
          Name: Constants.ID_ATTRIBUTE_NAME,
          ConstructorArgs: ImmutableArray.Create("id"),
          InitializerArgs: ImmutableArray<string>.Empty
        )
      )
    };

    var writer = TypeGenerator.CreateCodeWriter();

    type.WriteMetadata(writer, true);
    _type.WriteMetadata(writer, true);

    writer.InnerWriter.ToString().ShouldBeOfType<string>();
  }
}
