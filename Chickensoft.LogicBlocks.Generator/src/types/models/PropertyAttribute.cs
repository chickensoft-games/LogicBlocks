namespace Chickensoft.LogicBlocks.Generator.Types.Models;

using System.Collections.Immutable;

/// <summary>
/// Represents an attribute applied to a property.
/// </summary>
/// <param name="Name">Name of the attribute.</param>
/// <param name="ArgExpressions">Attribute argument expressions.</param>
public record PropertyAttribute(
  string Name,
  ImmutableArray<string> ArgExpressions
);
