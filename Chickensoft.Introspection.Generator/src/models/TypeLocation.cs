namespace Chickensoft.Introspection.Generator.Models;

using System;
using System.Collections.Immutable;
using System.Linq;
using Chickensoft.Introspection.Generator.Utils;

/// <summary>
/// Represents the location of a type inside any namespaces and/or containing
/// types.
/// </summary>
/// <param name="Namespaces">Namespaces containing the type.</param>
/// <param name="ContainingTypes">Containing type names.</param>
public sealed record TypeLocation(
  ImmutableArray<string> Namespaces,
  ImmutableArray<TypeReference> ContainingTypes
) {
  /// <summary>Fully resolved namespace of the type's location.</summary>
  public string Namespace => string.Join(".", Namespaces);

  /// <summary>Type prefix, used to generate the fully qualified name.</summary>
  public string Prefix {
    get {
      var prefix = FullNameOpen;
      if (prefix is not "") {
        prefix += ".";
      }
      return prefix;
    }
  }

  /// <summary>
  /// Full name of the type that represents this location.
  /// </summary>
  public string FullNameOpen => string.Join(
    ".",
    Namespaces.Concat(ContainingTypes.Select(t => t.SimpleNameOpen))
  );

  /// <summary>
  /// True if the location is within a generic type.
  /// </summary>
  public bool IsInGenericType => ContainingTypes.Any(t => t.IsGeneric);

  /// <summary>
  /// True if the location is not in a nested type, or nested only within
  /// only partial types.
  /// </summary>
  public bool IsFullyPartialOrNotNested =>
    ContainingTypes.Length == 0 || ContainingTypes.All(t => t.IsPartial);

  public bool Equals(TypeLocation? other) =>
    other is not null &&
    Namespaces.SequenceEqual(other.Namespaces) &&
    ContainingTypes.SequenceEqual(other.ContainingTypes);

  public override int GetHashCode() => HashCode.Combine(
    Namespaces, ContainingTypes
  );
}
