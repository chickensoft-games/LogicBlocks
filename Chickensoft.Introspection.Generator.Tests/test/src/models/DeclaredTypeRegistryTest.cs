namespace Chickensoft.Introspection.Generator.Tests.Models;

using System.Collections.Immutable;
using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class DeclaredTypeRegistryTest {
  [Fact]
  public void Equality() {
    var registry = new DeclaredTypeRegistry(
      allTypes: ImmutableDictionary<string, DeclaredType>.Empty,
      visibleTypes: ImmutableHashSet<DeclaredType>.Empty
    );

    registry.GetHashCode().ShouldBeOfType<int>();

    registry.Equals(null).ShouldBeFalse();

    registry.ShouldBe(
      new DeclaredTypeRegistry(
        allTypes: ImmutableDictionary<string, DeclaredType>.Empty,
        visibleTypes: ImmutableHashSet<DeclaredType>.Empty
      )
    );

    new DeclaredTypeRegistry(
      allTypes: ImmutableDictionary<string, DeclaredType>.Empty,
      visibleTypes: ImmutableHashSet<DeclaredType>.Empty
    ).ShouldNotBe(
      new DeclaredTypeRegistry(
        allTypes: ImmutableDictionary<string, DeclaredType>.Empty,
        visibleTypes: new DeclaredType[] {
          new(
            Reference: new TypeReference(
              "a",
              Construction: Construction.Class,
              IsPartial: false,
              TypeParameters: ImmutableArray<string>.Empty
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
          )
        }.ToImmutableHashSet()
      )
    );
  }
}
