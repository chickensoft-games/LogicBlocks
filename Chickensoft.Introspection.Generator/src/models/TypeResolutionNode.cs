namespace Chickensoft.Introspection.Generator.Models;

using System.Collections.Generic;

/// <summary>
/// Represents a namespace or type in a type name resolution tree.
/// </summary>
/// <param name="Name">Name of the namespace or type.</param>
/// <param name="TypeChildren">Map of type names to type nodes.</param>
public abstract record TypeResolutionNode(
  string Name, Dictionary<string, TypeNode> TypeChildren
);

/// <summary>
/// Represents a type in the type resolution tree.
/// </summary>
/// <param name="Name">Name of the type.</param>
/// <param name="IsVisible">Whether the type is visible from the top-level of
/// the project.</param>
/// <param name="IsConcrete">Whether the type is a concrete type or not.
/// </param>
/// <param name="OpenGenerics">True if the type node contains open generics.</param>
/// <param name="TypeChildren">Map of type names to type nodes.</param>
public record TypeNode(
  string Name,
  bool IsVisible,
  bool IsConcrete,
  string OpenGenerics,
  Dictionary<string, TypeNode> TypeChildren
) : TypeResolutionNode(Name, TypeChildren);

/// <summary>
/// Represents a namespace in a type tree.
/// </summary>
/// <param name="Name">Namespace name.</param>
/// <param name="Children">Map of child namespaces names to child namespaces.
/// </param>
/// <param name="TypeChildren">Map of type names to type nodes.</param>
public record NamespaceNode(
  string Name,
  Dictionary<string, NamespaceNode> Children,
  Dictionary<string, TypeNode> TypeChildren
) : TypeResolutionNode(Name, TypeChildren);
