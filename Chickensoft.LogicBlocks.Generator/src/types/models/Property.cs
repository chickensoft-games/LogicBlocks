namespace Chickensoft.LogicBlocks.Generator.Types.Models;

using System.Collections.Immutable;

/// <summary>
/// Represents a property on a metatype. Properties are opt-in and persisted.
/// </summary>
/// <param name="Name">Name of the property.</param>
/// <param name="Type">Type of the property.</param>
/// <param name="HasSetter">True if the property has a setter.</param>
/// <param name="IsNullable">True if the property is nullable.</param>
/// <param name="Attributes">Attributes applied to the property.</param>
public record Property(
  string Name,
  string Type,
  bool HasSetter,
  bool IsNullable,
  ImmutableArray<PropertyAttribute> Attributes
);
