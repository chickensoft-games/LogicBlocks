namespace Chickensoft.Introspection.Generator.Types.Models;

using System.Collections.Immutable;

/// <summary>
/// Data used to generate source output. Represents the finished work of the
/// generator before it is converted into source code output.
/// </summary>
/// <param name="AllTypes">All identified types.</param>
/// <param name="Metatypes">Valid metatypes that were found in the project's
/// source code.</param>
/// <param name="VisibleTypes">Declared types that are visible at the top-level
/// of the project's source code.</param>
/// <param name="ConcreteVisibleTypes">Declared types that are visible
/// at the top level of the project's source code and are not interfaces, static
/// classes, or abstract types.</param>
/// <param name="Mixins">Map of declared types that are marked with the mixin
/// attribute by their type name.</param>
public record GenerationData(
  ImmutableDictionary<string, DeclaredType> AllTypes,
  ImmutableDictionary<string, DeclaredType> Metatypes,
  ImmutableDictionary<string, DeclaredType> VisibleTypes,
  ImmutableDictionary<string, DeclaredType> ConcreteVisibleTypes,
  ImmutableDictionary<string, DeclaredType> Mixins
);
