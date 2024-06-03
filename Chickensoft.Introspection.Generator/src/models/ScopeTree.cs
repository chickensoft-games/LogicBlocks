namespace Chickensoft.Introspection.Generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ScopeTree {
  public NamespaceNode Root { get; } = new NamespaceNode(
    null, "", new(), new()
  );

  /// <summary>Map of types by their full, open generic names.</summary>
  public IDictionary<string, DeclaredType> TypesByFullNameOpen { get; }

  public ScopeTree(IDictionary<string, DeclaredType> typesByFullNameOpen) {
    TypesByFullNameOpen = typesByFullNameOpen;
    InitializeTree();
  }

  private void InitializeTree() {
    foreach (var declaredType in TypesByFullNameOpen.Values) {
      AddType(declaredType);
    }
  }

  private void AddType(DeclaredType type) {
    // Walk the namespaces tree. Once we have walked / constructed
    // namespaces, we'll be in the right spot to add the type.

    var currentNs = Root;
    foreach (var @namespace in type.Location.Namespaces) {
      if (!currentNs.Children.TryGetValue(@namespace, out var childNs)) {
        // Make namespace if it doesn't exist in the tree yet.

        childNs = new NamespaceNode(
          Parent: currentNs,
          Name: @namespace,
          Children: [],
          TypeChildren: []
        );

        currentNs.Children.Add(key: @namespace, value: childNs);
      }

      // Drill down into namespace which is now guaranteed to exist.
      currentNs = currentNs.Children[@namespace];
    }

    // Walk the type tree to account for containing types, creating them
    // if needed.
    ScopeNode current = currentNs;

    foreach (var containingType in GetContainingTypes(type)) {
      // Ensure that the type has a corresponding entry in the tree.
      if (
        !current.TypeChildren.TryGetValue(
          containingType.Reference.SimpleNameOpen, out var child
        )
      ) {
        child = new TypeNode(
          Parent: current,
          Type: containingType,
          TypeChildren: new()
        );

        current.TypeChildren.Add(
          key: containingType.Reference.SimpleNameOpen,
          value: child
        );
      }

      current = child;
    }

    if (current.TypeChildren.ContainsKey(type.Reference.SimpleNameOpen)) {
      // Type already in the tree. Do nothing.
      return;
    }

    // Add the type to the tree.
    var typeNode = new TypeNode(
      Parent: current, Type: type, TypeChildren: new()
    );

    current.TypeChildren.Add(
      key: type.Reference.SimpleNameOpen,
      value: typeNode
    );
  }

  /// <summary>
  /// Get all types visible from the top level scope that match the given
  /// predicate by searching the scope tree recursively.
  /// </summary>
  /// <param name="predicate">Predicate that a type must satisfy.</param>
  /// <param name="searchGenericTypes">Whether to search generic types (default
  /// is true).</param>
  /// <param name="searchPrivateTypes">Whether to search types that are not
  /// visible from the global scope (default is false).</param>
  /// <returns>Enumeration of declared types matching the predicate.</returns>
  public IEnumerable<DeclaredType> GetTypes(
    Func<TypeNode, bool>? predicate = null,
    bool searchGenericTypes = true,
    bool searchPrivateTypes = false
  ) => GetTypes(
    node: Root,
    predicate: predicate ?? (_ => true),
    generic: searchGenericTypes,
    @private: searchPrivateTypes
  ).OrderBy(t => t.FullNameOpen);

  private IEnumerable<DeclaredType> GetTypes(
    ScopeNode node,
    Func<TypeNode, bool> predicate,
    bool generic = true,
    bool @private = false
  ) {
    if (node is NamespaceNode nsNode) {
      // Recurse into child namespaces.
      foreach (var childNs in nsNode.Children.Values) {
        foreach (var type in GetTypes(childNs, predicate, generic, @private)) {
          yield return type;
        }
      }

      // Recurse into types contained at this namespace.
      foreach (var child in node.TypeChildren.Values) {
        foreach (var type in GetTypes(child, predicate, generic, @private)) {
          yield return type;
        }
      }
    }

    if (node is not TypeNode typeNode) {
      yield break;
    }

    if (typeNode.Type.IsGeneric && !generic) {
      // Type is generic and we're not supposed to search generic types.
      yield break;
    }

    if (!typeNode.Type.IsPublicOrInternal && !@private) {
      // Type is not visible from the top-level scope and we're not supposed
      // to search non-visible types.
      yield break;
    }

    if (predicate(typeNode)) {
      // Type satisfies predicate.
      yield return typeNode.Type;
    }

    // Recurse into child types.
    foreach (var child in node.TypeChildren.Values) {
      foreach (var type in GetTypes(child, predicate, generic)) {
        yield return type;
      }
    }
  }

  /// <summary>
  /// Attempts to resolve a reference to a type relative to the scope of the
  /// given type. For example, this can take a relative type reference for
  /// a base class and determine the fully qualified name of the base class and
  /// return that type.
  /// </summary>
  /// <param name="globalUsings">Global using directives across the project.
  /// </param>
  /// <param name="type">Type whose scope should be searched.</param>
  /// <param name="typeReference">Relative or fully qualified reference to a
  /// type in that scope.</param>
  /// <returns>The declared type the reference is referring to, or null if
  /// the reference could not be resolved.</returns>
  public DeclaredType? ResolveTypeReference(
    IEnumerable<UsingDirective> globalUsings,
    DeclaredType type,
    string reference
  ) {
    reference = reference.Replace("global::", "");
    var referenceParts = reference.Split('.');

    // Keep an ordered list of the nodes to search.
    var nodes = new LinkedList<ScopeNode>();

    // Map of using directive aliases to the type resolution node they alias.
    var aliasedTypes = new Dictionary<string, TypeNode>();

    // First, add the scope of the containing types to search.
    // We search containing types from innermost to outermost.
    foreach (var containingType in GetContainingTypes(type)) {
      if (GetNode(containingType) is { } node) {
        nodes.AddFirst(node);
      }
    }

    // Next, Enqueue the namespace of the type itself
    if (GetNode(type.Location.Namespace) is { } nsNode) {
      nodes.AddLast(nsNode);
    }

    // Then, add the scopes indicated by the using directives.
    foreach (var @using in type.Usings.Union(globalUsings)) {
      if (GetNode(@using.Name) is not { } node) {
        continue;
      }

      if (@using.Alias is { } alias && node is TypeNode typeNode) {
        aliasedTypes[alias] = typeNode;
        continue;
      }

      nodes.AddLast(node);
    }

    // Search aliases before anything else.
    foreach (var alias in aliasedTypes.Keys) {
      if (
        GetTypeByAliasReference(
          reference: reference,
          alias: alias,
          aliasedTypeNode: aliasedTypes[alias]
        ) is { } aliasedType
      ) {
        return aliasedType;
      }
    }

    // Search the nodes in order.
    foreach (var node in nodes) {
      // Recursively find types inside the scope of this node.
      var candidate = GetTypes(
        node,
        predicate:
          n => FullNameMatchesReferenceParts(n.Type.FullNameOpen, referenceParts),
        generic: false,
        @private: false
      ).FirstOrDefault();

      if (candidate is not null) {
        return candidate;
      }
    }

    // Lastly, see if the reference is a fully qualified type name.
    if (TypesByFullNameOpen.TryGetValue(reference, out var qualifiedType)) {
      return qualifiedType;
    }

    return null;
  }

  private DeclaredType? GetTypeByAliasReference(
    string reference,
    string alias,
    TypeNode aliasedTypeNode
  ) {
    if (reference == alias) {
      // Type reference is nothing but the alias. Easy.
      return aliasedTypeNode.Type;
    }

    if (
        reference.StartsWith(alias + ".") &&
        reference.Substring(alias.Length + 1) is { } path &&
        aliasedTypeNode.Type.FullNameOpen + "." + path is { } fullPath &&
        TypesByFullNameOpen.TryGetValue(fullPath, out var aliasedType)
      ) {
      // Type reference refers to the alias, plus some.
      return aliasedType;
    }

    return null;
  }

  private ScopeNode? GetNode(DeclaredType type) =>
    GetNode(type.FullNameOpen);

  private ScopeNode? GetNode(string fullNameOpen) =>
    GetNode(new Queue<string>(fullNameOpen.Split('.')));

  private ScopeNode? GetNode(Queue<string> nameParts) {
    ScopeNode current = Root;

    // Walk the namespaces.
    while (
      nameParts.Count > 0 &&
      current is NamespaceNode nsNode &&
      nsNode.Children.TryGetValue(nameParts.Peek(), out var next)
    ) {
      nameParts.Dequeue();
      current = next;
    }

    // Walk the types at the namespace we ended up in.
    while (
      nameParts.Count > 0 &&
      current.TypeChildren.TryGetValue(nameParts.Peek(), out var next)
    ) {
      nameParts.Dequeue();
      current = next;
    }

    return nameParts.Count == 0 ? current : null;
  }

  private IEnumerable<DeclaredType> GetContainingTypes(DeclaredType type) {
    var containingTypes = new StringBuilder();
    foreach (var containingTypeRef in type.Location.ContainingTypes) {
      containingTypes.Append(containingTypeRef.SimpleNameOpen);

      var fullNameOpen =
        (string.IsNullOrEmpty(type.Location.Namespace)
          ? ""
          : type.Location.Namespace + "."
        ) + containingTypes.ToString();

      containingTypes.Append(".");

      // Ensure that the containing type exists in the table of all types.
      if (
        !TypesByFullNameOpen.TryGetValue(fullNameOpen, out var containingType)
      ) {
        throw new InvalidOperationException(
          "Could not find the containing type " +
          $"`{fullNameOpen}` of `{type.FullNameOpen}`."
        );
      }

      yield return containingType;
    }
  }

  private bool FullNameMatchesReferenceParts(
    string fullName,
    IList<string> referenceParts
  ) {
    var fullNameParts = fullName.Split('.');

    if (
      fullNameParts.Length < referenceParts.Count
    ) {
      return false;
    }

    for (var i = referenceParts.Count - 1; i >= 0; i--) {
      if (
        referenceParts[i] !=
          fullNameParts[fullNameParts.Length - referenceParts.Count + i]
      ) {
        return false;
      }
    }

    return true;
  }
}
