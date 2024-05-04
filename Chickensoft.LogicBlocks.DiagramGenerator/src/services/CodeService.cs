namespace Chickensoft.LogicBlocks.DiagramGenerator.Services;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public interface ICodeService {
  /// <summary>
  /// Determines the fully qualified name of a named type symbol.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fallbackName">Default symbol name to return if there is no
  /// symbol name.</param>
  /// <returns>The fully qualified name of the symbol.</returns>
  string GetNameFullyQualified(INamedTypeSymbol? symbol, string fallbackName);

  /// <summary>
  /// Determines the fully qualified name of a named type symbol, without
  /// generic arguments.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fallbackName">Default symbol name to return if there is no
  /// symbol name.</param>
  /// <returns>The fully qualified name of the symbol.</returns>
  string GetNameFullyQualifiedWithoutGenerics(
    ITypeSymbol? symbol, string fallbackName
  );

  /// <summary>
  /// Get all the nested types of a type.
  /// </summary>
  /// <param name="symbol">Type to search.</param>
  /// <param name="predicate">Filter predicate.</param>
  /// <returns>Matching nested types.</returns>
  IEnumerable<INamedTypeSymbol> GetAllNestedTypesRecursively(
    INamedTypeSymbol symbol,
    Func<INamedTypeSymbol, bool> predicate
  );

  /// <summary>
  /// Gets all the base types of a given type.
  /// </summary>
  /// <param name="type">Type to examine.</param>
  /// <returns>Enumerable sequence of base types.</returns>
  IEnumerable<INamedTypeSymbol> GetAllBaseTypes(INamedTypeSymbol type);
}

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public class CodeService : ICodeService {
  public string GetNameFullyQualified(
    INamedTypeSymbol? symbol, string fallbackName
  ) => symbol?.ToDisplayString(
    SymbolDisplayFormat.FullyQualifiedFormat
  ) ?? fallbackName;

  public string GetNameFullyQualifiedWithoutGenerics(
    ITypeSymbol? symbol, string fallbackName
  ) => symbol?.ToDisplayString(
    SymbolDisplayFormat.FullyQualifiedFormat
      .WithGenericsOptions(SymbolDisplayGenericsOptions.None)
  ) ?? fallbackName;

  public IEnumerable<INamedTypeSymbol> GetAllNestedTypesRecursively(
    INamedTypeSymbol symbol,
    Func<INamedTypeSymbol, bool>? predicate = null
  ) {
    predicate ??= (_) => true;
    foreach (var type in @symbol.GetTypeMembers()) {
      if (predicate(type)) { yield return type; }
      foreach (var nestedType in GetAllNestedTypesRecursively(type, predicate)) {
        if (predicate(nestedType)) {
          yield return nestedType;
        }
      }
    }
  }

  public IEnumerable<INamedTypeSymbol> GetAllBaseTypes(INamedTypeSymbol type) {
    var current = type;
    while (current.BaseType != null) {
      yield return current.BaseType;
      current = current.BaseType;
    }
  }
}
