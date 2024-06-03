namespace Chickensoft.Introspection.Generator.Tests.Models;

using System;
using System.Collections.Immutable;
using Chickensoft.Collections;
using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class ScopeTreeTest {
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
  BaseType: null,
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
    BaseType: null,
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
    BaseType: null,
    Usings: ImmutableHashSet<UsingDirective>.Empty,
    Kind: DeclaredTypeKind.ConcreteType,
    IsStatic: false,
    IsPublicOrInternal: true,
    Properties: ImmutableArray<DeclaredProperty>.Empty,
    Attributes: ImmutableArray<DeclaredAttribute>.Empty,
    Mixins: ImmutableArray<string>.Empty
  );

  private readonly DeclaredType _typeInOtherNs = new(
    Reference: new TypeReference(
      "OtherType",
      Construction: Construction.Class,
      IsPartial: false,
      TypeParameters: ImmutableArray<string>.Empty
    ),
    SyntaxLocation: Microsoft.CodeAnalysis.Location.None,
    Location: new TypeLocation(
      Namespaces: ImmutableArray.Create("A", "B", "C"),
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

  private readonly DeclaredType _typeExtendingTypeInOtherNs = new(
    Reference: new TypeReference(
      "OtherTypeChild",
      Construction: Construction.Class,
      IsPartial: false,
      TypeParameters: ImmutableArray<string>.Empty
    ),
    SyntaxLocation: Microsoft.CodeAnalysis.Location.None,
    Location: new TypeLocation(
      Namespaces: ImmutableArray<string>.Empty,
      ContainingTypes: ImmutableArray<TypeReference>.Empty
    ),
    BaseType: "OtherType",
    Usings: ImmutableHashSet.Create(
      new UsingDirective(Alias: null, Name: "A.B.C", false, false, false)
    ),
    Kind: DeclaredTypeKind.ConcreteType,
    IsStatic: false,
    IsPublicOrInternal: true,
    Properties: ImmutableArray<DeclaredProperty>.Empty,
    Attributes: ImmutableArray<DeclaredAttribute>.Empty,
    Mixins: ImmutableArray<string>.Empty
  );

  [Fact]
  public void AddsDeclaredTypes() {
    // Use a map to guarantee types are added in the order shown below.
    var tree = new ScopeTree(
      new Map<string, DeclaredType>() {
        [_inner.FullNameOpen] = _inner,
        [_outer.FullNameOpen] = _outer,
        [_genericOuter.FullNameOpen] = _genericOuter
      }
    );

    tree
      .GetTypes((_) => false, searchGenericTypes: false)
      .ShouldBeEmpty();

    tree
      .GetTypes((_) => true, searchGenericTypes: true)
      .ShouldBe(
        [
          _inner,
          _outer,
          _genericOuter
        ],
        ignoreOrder: true
    );
  }

  [Fact]
  public void ThrowsIfContainingTypeIsNotAbleToBeFound() =>
    Should.Throw<InvalidOperationException>(
      () => new ScopeTree(
        new Map<string, DeclaredType>() {
          [_inner.FullNameOpen] = _inner,
          [_outer.FullNameOpen] = _outer
        }
      )
    );

  [Fact]
  public void FindsTypesInScope() {
    var tree = new ScopeTree(
      new Map<string, DeclaredType>() {
        [_typeInOtherNs.FullNameOpen] = _typeInOtherNs,
        [_typeExtendingTypeInOtherNs.FullNameOpen] = _typeExtendingTypeInOtherNs,
      }
    );

    tree.ResolveTypeReference([], _typeExtendingTypeInOtherNs, "OtherType")
      .ShouldNotBeNull()
      .FullNameOpen
      .ShouldBe("A.B.C.OtherType");
  }
}
