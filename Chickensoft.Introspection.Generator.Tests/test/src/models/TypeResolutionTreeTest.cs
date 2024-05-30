namespace Chickensoft.Introspection.Generator.Tests.Models;

using System;
using System.Collections.Immutable;
using Chickensoft.Collections;
using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class TypeResolutionTreeTest {
  private readonly DeclaredType _genericOuter = new(
  Reference: new TypeReference(
    "GenericOuter",
    Construction: Construction.Class,
    IsPartial: false,
    TypeParameters: new[] { "T" }.ToImmutableArray()
  ),
  SyntaxLocation: Microsoft.CodeAnalysis.Location.None,
  Location: new TypeLocation(
    Namespaces: ImmutableArray<string>.Empty,
    ContainingTypes: ImmutableArray<TypeReference>.Empty
  ),
  Usings: ImmutableHashSet<UsingDirective>.Empty,
  Kind: DeclaredTypeKind.ConcreteType,
  IsStatic: false,
  IsPublicOrInternal: true,
  Properties: ImmutableArray<DeclaredProperty>.Empty,
  Attributes: ImmutableArray<DeclaredAttribute>.Empty,
  Mixins: ImmutableArray<string>.Empty
);

  private readonly DeclaredType _outer = new(
    Reference: new TypeReference(
      "Outer",
      Construction: Construction.Class,
      IsPartial: false,
      TypeParameters: ImmutableArray<string>.Empty
    ),
    SyntaxLocation: Microsoft.CodeAnalysis.Location.None,
    Location: new TypeLocation(
      Namespaces: ImmutableArray<string>.Empty,
      ContainingTypes: new[] {
        // Nested inside _outer
        new TypeReference(
          "GenericOuter",
          Construction.Class,
          false,
          new[] { "T" }.ToImmutableArray()
        )
      }.ToImmutableArray()
    ),
    Usings: ImmutableHashSet<UsingDirective>.Empty,
    Kind: DeclaredTypeKind.ConcreteType,
    IsStatic: false,
    IsPublicOrInternal: true,
    Properties: ImmutableArray<DeclaredProperty>.Empty,
    Attributes: ImmutableArray<DeclaredAttribute>.Empty,
    Mixins: ImmutableArray<string>.Empty
  );

  private readonly DeclaredType _inner = new(
    Reference: new TypeReference(
      "Inner",
      Construction: Construction.Class,
      IsPartial: false,
      TypeParameters: ImmutableArray<string>.Empty
    ),
    SyntaxLocation: Microsoft.CodeAnalysis.Location.None,
    Location: new TypeLocation(
      Namespaces: ImmutableArray<string>.Empty,
      ContainingTypes: new[] {
        // Nested inside _outer
        new TypeReference(
          "GenericOuter",
          Construction.Class,
          false,
          TypeParameters: new[] { "T" }.ToImmutableArray()
        ),
        new TypeReference(
          "Outer",
          Construction.Class,
          false,
          ImmutableArray<string>.Empty
        )
      }.ToImmutableArray()
    ),
    Usings: ImmutableHashSet<UsingDirective>.Empty,
    Kind: DeclaredTypeKind.ConcreteType,
    IsStatic: false,
    IsPublicOrInternal: true,
    Properties: ImmutableArray<DeclaredProperty>.Empty,
    Attributes: ImmutableArray<DeclaredAttribute>.Empty,
    Mixins: ImmutableArray<string>.Empty
  );

  [Fact]
  public void AddsDeclaredTypes() {
    var tree = new TypeResolutionTree();

    // Use a map to guarantee types are added in the order shown below.
    tree.AddDeclaredTypes(new Map<string, DeclaredType>() {
      [_inner.FullNameOpen] = _inner,
      [_outer.FullNameOpen] = _outer,
      [_genericOuter.FullNameOpen] = _genericOuter
    });

    tree
      .GetVisibleTypes((_) => false, searchGenericTypes: false)
      .ShouldBeEmpty();

    tree
      .GetVisibleTypes((_) => true, searchGenericTypes: true)
      .ShouldBe(
        [
          _inner.FullNameOpen,
          _outer.FullNameOpen,
          _genericOuter.FullNameOpen
        ],
        ignoreOrder: true
    );
  }

  [Fact]
  public void ThrowsIfContainingTypeIsNotAbleToBeFound() {
    var tree = new TypeResolutionTree();

    Should.Throw<InvalidOperationException>(
      () => tree.AddDeclaredTypes(
        new Map<string, DeclaredType>() {
          [_inner.FullNameOpen] = _inner,
          [_outer.FullNameOpen] = _outer
        }
      )
    );
  }
}
