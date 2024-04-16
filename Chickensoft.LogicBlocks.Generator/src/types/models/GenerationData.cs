namespace Chickensoft.LogicBlocks.Generator.Types.Models;

using System.Collections.Immutable;

/// <summary>
/// Data used to generate source output. Represents the finished work of the
/// generator before it is converted into source code output.
/// </summary>
/// <param name="Metatypes">Valid metatypes that were found in the project's
/// source code.</param>
/// <param name="VisibleTypes">Declared types that are visible at the top-level
/// of the project's source code.</param>
/// <param name="VisibleInstantiableTypes">Declared types that are visible
/// at the top level of the project's source code and are not interfaces, static
/// classes, or abstract types.</param>
public record GenerationData(
  ImmutableDictionary<string, DeclaredTypeInfo> Metatypes,
  ImmutableDictionary<string, DeclaredTypeInfo> VisibleTypes,
  ImmutableDictionary<string, DeclaredTypeInfo> VisibleInstantiableTypes
);
