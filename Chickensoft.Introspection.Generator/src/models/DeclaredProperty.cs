namespace Chickensoft.Introspection.Generator.Models;

using System.Collections.Immutable;

/// <summary>
/// Represents a property on a metatype. Properties are opt-in and persisted.
/// </summary>
/// <param name="Name">Name of the property.</param>
/// <param name="HasSetter">True if the property has a setter.</param>
/// <param name="IsNullable">True if the property is nullable.</param>
/// <param name="GenericType">Type of the property.</param>
/// <param name="Attributes">Attributes applied to the property.</param>
public record DeclaredProperty(
  string Name,
  bool HasSetter,
  bool IsNullable,
  GenericTypeNode GenericType,
  ImmutableArray<DeclaredAttribute> Attributes
);
