namespace Chickensoft.Introspection.Generator.Models;

using System.Collections.Immutable;

/// <summary>
/// Represents an attribute applied to a property.
/// </summary>
/// <param name="Name">Name of the attribute.</param>
/// <param name="ConstructorArgs">Attribute constructor arguments.</param>
/// <param name="InitializerArgs">Attribute initializer arguments (not part
/// of the constructor signature, but settable properties from object
/// initializer syntax).</param>
public record DeclaredAttribute(
  string Name,
  ImmutableArray<string> ConstructorArgs,
  ImmutableArray<string> InitializerArgs
);
