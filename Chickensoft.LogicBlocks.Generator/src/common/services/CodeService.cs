namespace Chickensoft.LogicBlocks.Generator.Common.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public interface ICodeService {
  /// <summary>
  /// Determines the list of visible type parameters shown on a class
  /// declaration syntax node.
  /// </summary>
  /// <param name="classDeclaration">Class declaration syntax node.</param>
  /// <returns>Visible list of type parameters shown on that particular class
  /// declaration syntax node.</returns>
  ImmutableArray<string> GetTypeParameters(
    ClassDeclarationSyntax classDeclaration
  );

  /// <summary>
  /// Determines the list of visible interfaces shown on a class declaration
  /// syntax node and returns the set of the fully qualified interface names.
  /// </summary>
  /// <param name="classDeclaration">Class declaration syntax node.</param>
  /// <param name="symbol">Named type symbol corresponding to the class
  /// </param>
  /// <returns>Visible list of interfaces shown on that particular class
  /// declaration syntax node.</returns>
  ImmutableArray<string> GetVisibleInterfacesFullyQualified(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  );

  /// <summary>
  /// Determines the list of visible generic interfaces shown on a class
  /// declaration syntax node.
  /// </summary>
  /// <param name="classDeclaration">Class declaration syntax node.</param>
  /// <returns>Visible list of generic interfaces shown on that particular class
  /// declaration syntax node.</returns>
  ImmutableArray<string> GetVisibleGenericInterfaces(
    ClassDeclarationSyntax classDeclaration
  );

  /// <summary>
  /// Determines the fully resolved containing namespace of a symbol, if the
  /// symbol is non-null and has a containing namespace. Otherwise, returns a
  /// blank string.
  /// </summary>
  /// <param name="symbol">A potential symbol whose containing namespace
  /// should be determined.</param>
  /// <returns>The fully resolved containing namespace of the symbol, or the
  /// empty string.</returns>
  string? GetContainingNamespace(ISymbol? symbol);

  /// <summary>
  /// Computes the "using" imports of the syntax tree that the given named type
  /// symbol resides in.
  /// </summary>
  /// <param name="symbol">Named type to inspect.</param>
  /// <returns>String array of "using" imports.</returns>
  ImmutableHashSet<string> GetUsings(INamedTypeSymbol? symbol);

  /// <summary>
  /// Recursively computes the base classes of a named type symbol.
  /// </summary>
  /// <param name="symbol">Named type to inspect.</param>
  /// <returns>String array of fully qualified base classes, or an empty array
  /// if no base classes.</returns>
  ImmutableArray<string> GetBaseClassHierarchy(INamedTypeSymbol? symbol);

  /// <summary>
  /// Given an optional named type symbol, returns the fully qualified name of
  /// the base type, or null if the symbol is null or has no known base type.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fallbackClass">Default class name to return if there is no
  /// base class.</param>
  /// <returns>Fully qualified name.</returns>
  string GetBaseTypeFullyQualified(
    INamedTypeSymbol? symbol, string fallbackClass = "object"
  );

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
  /// Returns the array of members on the named type symbol, if any.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <returns>Array of members.</returns>
  ImmutableArray<ISymbol> GetMembers(INamedTypeSymbol? symbol);

  /// <summary>
  /// Returns the name of the symbol, or the name of the identifier for the
  /// given type declaration syntax node if the symbol is null.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fallbackType">Fallback type declaration syntax node.</param>
  string GetName(
    INamedTypeSymbol? symbol, TypeDeclarationSyntax fallbackType
  );

  /// <summary>
  /// Gets the name of the symbol, or null if the symbol is null.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <returns>Name of the symbol.</returns>
  string? GetName(ISymbol? symbol);

  /// <summary>
  /// Finds the attribute on the symbol with the given full name, such as
  /// <c>SuperNodeAttribute</c> or <c>ExportAttribute</c>.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fullName">Full name of the attribute.</param>
  /// <returns></returns>
  AttributeData? GetAttribute(
    INamedTypeSymbol? symbol, string fullName
  );

  /// <summary>
  /// Gets all nested types in a type that extend a given ancestor type.
  /// </summary>
  /// <param name="containingType">Type which contains nested types to search.
  /// </param>
  /// <param name="ancestorType">The ancestor the nested types must subclass
  /// to be returned.</param>
  IEnumerable<INamedTypeSymbol> GetNestedSubtypesExtending(
    INamedTypeSymbol containingType,
    INamedTypeSymbol ancestorType
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
  public ImmutableArray<string> GetVisibleInterfacesFullyQualified(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  ) {
    var nonGenericInterfaces = GetVisibleInterfaces(classDeclaration);
    var genericInterfaces = GetVisibleGenericInterfaces(classDeclaration);
    var visibleInterfaces = nonGenericInterfaces
      .Union(genericInterfaces)
      .ToImmutableArray();

    var allKnownInterfaces = symbol?.AllInterfaces ??
      ImmutableArray<INamedTypeSymbol>.Empty;

    if (allKnownInterfaces.IsEmpty) {
      // Symbol doesn't have any information (probably because the code isn't
      // fully valid while being edited), so just return the non-fully-qualified
      // names of the interfaces (since that's the best we can do).
      return visibleInterfaces;
    }

    // Find the fully qualified names of only the interfaces that are directly
    // listed on the class declaration syntax node we are given.
    // allKnownInterfaces is computed based on the semantic model, so it
    // actually can contain interfaces that may be implemented by other
    // partial class implementations elsewhere.
    return allKnownInterfaces
      .Where(@interface => visibleInterfaces.Contains(@interface.Name))
      .Select(
        @interface => @interface.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        )
      )
      .OrderBy(@interface => @interface)
      .ToImmutableArray();
  }

  public ImmutableArray<string> GetTypeParameters(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.TypeParameterList?.Parameters
      .Select(parameter => parameter.Identifier.ValueText)
      .ToImmutableArray()
    ) ?? ImmutableArray<string>.Empty;

  public ImmutableArray<string> GetVisibleInterfaces(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.BaseList?.Types
      .Select(type => type.Type)
      .OfType<IdentifierNameSyntax>()
      .Select(type => type.Identifier.ValueText)
      .OrderBy(@interface => @interface)
      .ToImmutableArray()
    ) ?? ImmutableArray<string>.Empty;

  public ImmutableArray<string> GetVisibleGenericInterfaces(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.BaseList?.Types
      .Select(type => type.Type)
      .OfType<GenericNameSyntax>()
      .Select(type => type.Identifier.ValueText)
      .ToImmutableArray()
    ) ?? ImmutableArray<string>.Empty;

  public string? GetContainingNamespace(ISymbol? symbol)
    => symbol?.ContainingNamespace.IsGlobalNamespace == true
      ? null
      : symbol?.ContainingNamespace.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        ).Replace("global::", "");

  public ImmutableArray<string> GetBaseClassHierarchy(INamedTypeSymbol? symbol)
    => symbol?.BaseType is INamedTypeSymbol baseSymbol
      ? new[] {
            baseSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        }.Concat(GetBaseClassHierarchy(baseSymbol)).ToImmutableArray()
      : ImmutableArray<string>.Empty;

  public ImmutableHashSet<string> GetUsings(INamedTypeSymbol? symbol) {
    if (symbol is null) {
      return ImmutableHashSet<string>.Empty;
    }
    var allUsings = SyntaxFactory.List<UsingDirectiveSyntax>();
    foreach (var syntaxRef in symbol.DeclaringSyntaxReferences) {
      foreach (var parent in syntaxRef.GetSyntax().Ancestors(false)) {
        if (parent is BaseNamespaceDeclarationSyntax ns) {
          allUsings = allUsings.AddRange(ns.Usings);
        }
        else if (parent is CompilationUnitSyntax comp) {
          allUsings = allUsings.AddRange(comp.Usings);
        }
      }
    }
    return allUsings
      .Select(@using => @using.Name.ToString())
      .ToImmutableHashSet();
  }

  public AttributeData? GetAttribute(
    INamedTypeSymbol? symbol, string fullName
  ) {
    var attributes = symbol?.GetAttributes()
      ?? ImmutableArray<AttributeData>.Empty;
    return attributes.FirstOrDefault(
      attribute => attribute.AttributeClass?.Name == fullName
    );
  }

  public bool HasAttributeFullyQualified(
    ImmutableArray<AttributeData> attributes, string fullyQualifiedName
  ) => attributes.Any(
      attribute =>
        attribute.AttributeClass?.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        ) == fullyQualifiedName
    );

  public string GetBaseTypeFullyQualified(
    INamedTypeSymbol? symbol, string fallbackClass = "object"
  ) => symbol?.BaseType?.ToDisplayString(
    SymbolDisplayFormat.FullyQualifiedFormat
  ) ?? fallbackClass;

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

  public string GetName(
    INamedTypeSymbol? symbol, TypeDeclarationSyntax fallbackType
  ) => symbol?.Name ?? fallbackType.Identifier.ValueText;

  public string? GetName(ISymbol? symbol) => symbol?.Name;

  public ImmutableArray<ISymbol> GetMembers(INamedTypeSymbol? symbol)
    => symbol?.GetMembers() ?? ImmutableArray<ISymbol>.Empty;

  public IEnumerable<INamedTypeSymbol> GetNestedSubtypesExtending(
    INamedTypeSymbol containingType,
    INamedTypeSymbol ancestorType
  ) => GetAllNestedTypesRecursively(
    containingType,
    (type) =>
      SymbolEqualityComparer.Default.Equals(type, ancestorType) ||
      GetAllBaseTypes(type).Any(
        (baseType) =>
          SymbolEqualityComparer.Default.Equals(ancestorType, baseType)
      )
  );

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
