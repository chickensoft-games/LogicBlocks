namespace Chickensoft.LogicBlocks.Generator.Types.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public record TypeResolutionTree {
  public NamespaceNode Root { get; } = new NamespaceNode(
    "global::", new(), new()
  );

  /// <summary>
  /// Adds all the types to the tree from a map of all declared types.
  /// </summary>
  /// <param name="declaredTypesByFullName">Map of declared types by their
  /// fully qualified names.</param>
  public void AddDeclaredTypes(
    Dictionary<string, DeclaredTypeInfo> declaredTypesByFullName
  ) {
    foreach (var declaredType in declaredTypesByFullName.Values) {
      AddDeclaredType(declaredType, declaredTypesByFullName);
    }
  }

  private void AddDeclaredType(
    DeclaredTypeInfo declaredType,
    Dictionary<string, DeclaredTypeInfo> declaredTypesByFullName
  ) {
    var fullNameBuilder = new StringBuilder();
    var currentNs = Root;

    // Drill into tree based on namespaces
    foreach (var namespacePart in declaredType.Location.Namespaces) {
      fullNameBuilder.Append(namespacePart + ".");
      if (!currentNs.Children.TryGetValue(namespacePart, out var child)) {
        child = new NamespaceNode(namespacePart, new(), new());
        currentNs.Children.Add(namespacePart, child);
      }
      currentNs = child;
    }

    TypeResolutionNode current = currentNs;

    // Drill down further based on containing types
    foreach (var containingTypeName in declaredType.Location.ContainingTypes) {
      fullNameBuilder.Append($"{containingTypeName}.");

      var containingDeclaredTypeFullName = fullNameBuilder.ToString();
      var containingDeclaredType =
        (declaredTypesByFullName
          ?.ContainsKey(containingDeclaredTypeFullName) ?? false
        )
        ? declaredTypesByFullName[containingDeclaredTypeFullName]
        : null;

      if (
        !current.TypeChildren.TryGetValue(containingTypeName, out var child)
      ) {
        child = new TypeNode(
          Name: containingTypeName,
          IsVisible: containingDeclaredType?.IsVisible ?? false,
          IsInstantiable:
            containingDeclaredType?.Kind == DeclaredTypeKind.InstantiableType,
          OpenGenerics: containingDeclaredType?.OpenGenerics ?? "",
          TypeChildren: new()
        );
        current.TypeChildren.Add(containingTypeName, child);
      }
      current = child;
    }

    // Add the type itself
    if (!current.TypeChildren.ContainsKey(declaredType.Name)) {
      var typeNode = new TypeNode(
        Name: declaredType.Name,
        IsVisible: declaredType.IsVisible,
        IsInstantiable: declaredType.Kind == DeclaredTypeKind.InstantiableType,
        OpenGenerics: declaredType.OpenGenerics,
        TypeChildren: new()
      );
      current.TypeChildren.Add(declaredType.Name, typeNode);
    }
  }

  /// <summary>Get the full names of all visible types.</summary>
  /// <param name="predicate">Optional predicate to filter types further.
  /// </param>
  /// <param name="searchGenericTypes">Whether to search for types within
  /// generic types.</param>
  /// <returns>Fully qualified names of visible types.</returns>
  public IEnumerable<string> GetVisibleTypes(
    Func<TypeNode, bool>? predicate = null, bool searchGenericTypes = true
  ) => GetVisibleTypes(
    Root, predicate ?? (_ => true), searchGenericTypes, null
  ).OrderBy(t => t);

  private IEnumerable<string> GetVisibleTypes(
    TypeResolutionNode node,
    Func<TypeNode, bool> predicate,
    bool searchGenericTypes,
    string? namePrefix
  ) {
    var name =
      $"{(namePrefix is not null and not "global::" ? namePrefix + "." : "")}{node.Name}";

    if (node is TypeNode typeNode) {
      name += typeNode.OpenGenerics;

      if (!typeNode.IsVisible) {
        yield break;
      }

      if (predicate(typeNode)) {
        yield return name;
      }

      if (typeNode.OpenGenerics != "" && !searchGenericTypes) {
        yield break;
      }

      foreach (var child in typeNode.TypeChildren.Values) {
        foreach (
          var fullName in GetVisibleTypes(
            child, predicate, searchGenericTypes, name
          )
        ) {
          yield return fullName;
        }
      }
    }
    else if (node is NamespaceNode nsNode) {
      foreach (var typeChild in nsNode.TypeChildren.Values) {
        foreach (
          var fullName in GetVisibleTypes(
            typeChild, predicate, searchGenericTypes, name
          )
        ) {
          yield return fullName;
        }
      }

      foreach (var child in nsNode.Children.Values) {
        foreach (
          var fullName in GetVisibleTypes(
            child, predicate, searchGenericTypes, name
          )
        ) {
          yield return fullName;
        }
      }
    }
  }
}
