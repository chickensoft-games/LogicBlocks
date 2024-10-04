namespace Chickensoft.SourceGeneratorUtils;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public static class SymbolExtensions {
  public static bool InheritsFromOrEquals(
      this ITypeSymbol type, INamedTypeSymbol baseType) =>
        type
          .GetBaseTypesAndThis()
          .Any(t => SymbolEqualityComparer.Default.Equals(t, baseType)) ||
          (
            baseType.IsGenericType &&
            type.GetBaseTypesAndThis().Any(t =>
              SymbolEqualityComparer.Default.Equals(
                t.OriginalDefinition,
                baseType.OriginalDefinition
              )
            )
          );

  private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(
    this ITypeSymbol? type
  ) {
    var current = type;
    while (current != null) {
      yield return current;
      current = current.BaseType;
    }
  }
}
