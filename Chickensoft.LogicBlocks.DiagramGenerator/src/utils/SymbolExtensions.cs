namespace Chickensoft.SourceGeneratorUtils;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public static class SymbolExtensions {
  public static bool InheritsFromOrEquals(
      this ITypeSymbol type, ITypeSymbol baseType) =>
        type
          .GetBaseTypesAndThis()
          .Any(t => SymbolEqualityComparer.Default.Equals(t, baseType));

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
