namespace Chickensoft.LogicBlocks.Generator.Types.Models;

using System.Collections.Immutable;

/// <summary>
/// Data used to generate source output. Represents the finished work of the
/// generator before it is converted into source code output.
/// </summary>
/// <param name="VisibleTypes">Type names that are visible at the top-level
/// of the project's source code.</param>
/// <param name="VisibleInstantiableTypes">Type names that are visible
/// at the top level of the project's source code and are not interfaces, static
/// classes, or abstract types.</param>
public record GenerationData(
  ImmutableHashSet<string> VisibleTypes,
  ImmutableHashSet<string> VisibleInstantiableTypes
);
