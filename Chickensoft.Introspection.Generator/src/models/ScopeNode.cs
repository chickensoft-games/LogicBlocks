namespace Chickensoft.Introspection.Generator.Models;

using System.Collections.Generic;

/// <summary>
/// Represents a namespace or type in a type name resolution tree.
/// </summary>
/// <param name="Parent">Parent node.</param>
/// <param name="Name">Name of the namespace or type.</param>
/// <param name="TypeChildren">Map of type names to type nodes.</param>
public abstract record ScopeNode(
  ScopeNode? Parent, string Name, Dictionary<string, TypeNode> TypeChildren
);

/// <summary>
/// Represents a type in the type resolution tree.
/// </summary>
/// <param name="Parent">Parent node.</param>
/// <param name="Type">Declared type.</param> 
/// <param name="TypeChildren">Map of type names to type nodes.</param>
public sealed record TypeNode(
  ScopeNode? Parent,
  DeclaredType Type,
  Dictionary<string, TypeNode> TypeChildren
) : ScopeNode(Parent, Type.Reference.SimpleNameOpen, TypeChildren);

/// <summary>
/// Represents a namespace in a type tree.
/// </summary>
/// <param name="Parent">Parent node.</param>
/// <param name="Name">Namespace name.</param>
/// <param name="Children">Map of child namespaces names to child namespaces.
/// </param>
/// <param name="TypeChildren">Map of type names to type nodes.</param>
public sealed record NamespaceNode(
    ScopeNode? Parent,
    string Name,
    Dictionary<string, NamespaceNode> Children,
    Dictionary<string, TypeNode> TypeChildren
) : ScopeNode(Parent, Name, TypeChildren);
