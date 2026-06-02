namespace Chickensoft.LogicBlocks.DiagramGenerator.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public interface ICodeService
{
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

  /// <summary>
  /// Gets all types in the compilation that inherit from the given symbol.
  /// </summary>
  /// <param name="baseSymbol">The base type to search for.</param>
  /// <param name="compilation">The compilation to search.</param>
  /// <param name="predicate">Filter predicate.</param>
  /// <returns>Matching derived types.</returns>
  IEnumerable<INamedTypeSymbol> GetAllDerivedTypes(
    INamedTypeSymbol baseSymbol,
    Compilation compilation,
    Func<INamedTypeSymbol, bool> predicate
  );

  bool InheritsFromByName(TypeDeclarationSyntax typeDeclarationSyntax, string logicBlockTypeName);
}

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public class CodeService : ICodeService
{
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
  )
  {
    predicate ??= (_) => true;
    foreach (var type in @symbol.GetTypeMembers())
    {
      if (predicate(type))
      { yield return type; }
      foreach (var nestedType in GetAllNestedTypesRecursively(type, predicate))
      {
        if (predicate(nestedType))
        {
          yield return nestedType;
        }
      }
    }
  }

  public IEnumerable<INamedTypeSymbol> GetAllBaseTypes(INamedTypeSymbol type)
  {
    var current = type;
    while (current.BaseType != null)
    {
      yield return current.BaseType;
      current = current.BaseType;
    }
  }

  public IEnumerable<INamedTypeSymbol> GetAllDerivedTypes(
    INamedTypeSymbol baseSymbol,
    Compilation compilation,
    Func<INamedTypeSymbol, bool>? predicate = null
  )
  {
    predicate ??= (_) => true;
    foreach (var type in GetAllTypesInNamespace(compilation.GlobalNamespace))
    {
      if (predicate(type))
      {
        yield return type;
      }
    }
  }

  private IEnumerable<INamedTypeSymbol> GetAllTypesInNamespace(INamespaceSymbol ns)
  {
    foreach (var member in ns.GetMembers())
    {
      switch (member)
      {
        case INamespaceSymbol childNamespace:
          {
            foreach (var type in GetAllTypesInNamespace(childNamespace))
            {
              yield return type;
            }
            break;
          }
        case INamedTypeSymbol type:
          {
            yield return type;
            foreach (var nestedType in GetAllNestedTypesRecursively(type))
            {
              yield return nestedType;
            }
            break;
          }
      }
    }
  }

  /// <summary>
  /// Recursively checks whether a type declaration inherits from a type with
  /// the given simple name, walking the syntax tree of the same file for
  /// intermediate base type declarations.
  /// </summary>
  public bool InheritsFromByName(
    TypeDeclarationSyntax typeDecl,
    string targetName
  )
  {
    var root = typeDecl.SyntaxTree.GetRoot();
    return InheritsFromByName(typeDecl, targetName, root, []);
  }

  private static bool InheritsFromByName(
    TypeDeclarationSyntax typeDecl,
    string targetName,
    SyntaxNode root,
    HashSet<string> visited
  )
  {
    if (!visited.Add(typeDecl.Identifier.Text)) { return false; }
    if (typeDecl.BaseList is null) { return false; }

    foreach (var baseTypeSyntax in typeDecl.BaseList.Types)
    {
      // Strip generics and namespace qualifiers to get the simple name.
      var rawName = baseTypeSyntax.Type.ToString();
      var simpleName = rawName.Split('<')[0].Split('.').Last();

      if (simpleName == targetName) { return true; }

      // Try to find the base type declaration in the same file and recurse.
      var baseDecl = root.DescendantNodes()
        .OfType<TypeDeclarationSyntax>()
        .FirstOrDefault(t => t.Identifier.Text == simpleName);

      if (baseDecl is not null &&
          InheritsFromByName(baseDecl, targetName, root, visited))
      {
        return true;
      }
    }

    return false;
  }
}
