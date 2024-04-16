
namespace Chickensoft.LogicBlocks.Generator.Types.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// Represents a declared type.
/// </summary>
/// <param name="Location">Location of the type in the source code.</param>
/// <param name="Kind">Kind of the type.</param>
/// <param name="HasMetatypeAttribute">True if the type was tagged with the
/// MetatypeAttribute.</param>
/// <param name="IsTopLevelAccessible">True if the public or internal
/// visibility modifier was seen on the type.</param>
/// <param name="IsPartial">True if the partial modifier was seen on the type.
/// </param>
/// <param name="NumTypeParameters">Number of type parameters the type has.
/// </param>
/// <param name="Name">Name of the type itself.</param>
/// <param name="Diagnostics">Diagnostics that were generated during generator
/// transformation.</param>
public record DeclaredTypeInfo(
  TypeReference Reference,
  TypeLocation Location,
  ImmutableHashSet<UsingDirective> Usings,
  DeclaredTypeKind Kind,
  bool HasMetatypeAttribute,
  bool IsTopLevelAccessible,
  ImmutableArray<Property> Properties,
  ImmutableHashSet<Diagnostic> Diagnostics
) {
  /// <summary>Output filename (only works for non-generic types).</summary>
  public string Filename => FullName.Replace('.', '_');

  /// <summary>
  /// Fully qualified name, as determined based on syntax nodes only.
  /// </summary>
  public string FullName =>
    Location.Prefix + Reference.Name + Reference.OpenGenerics;

  /// <summary>
  /// True if the metatype information can be generated for this type.
  /// </summary>
  public bool CanGenerateMetatypeInfo =>
    HasMetatypeAttribute && Reference.IsPartial && !IsGeneric;

  /// <summary>
  /// True if the type is generic. A type is generic if it has type parameters
  /// or is nested inside any containing types that have type parameters.
  /// </summary>
  public bool IsGeneric =>
    Reference.TypeParameters.Length > 0 ||
    Location.IsInGenericType;

  /// <summary>
  /// Merge this partial type definition with another partial type definition
  /// for the same type.
  /// </summary>
  /// <param name="declaredType">Declared type representing the same type.
  /// </param>
  /// <returns>Updated representation of the declared type.</returns>
  public DeclaredTypeInfo MergePartialDefinition(
    DeclaredTypeInfo declaredType
  ) => new(
    Reference.MergePartialDefinition(declaredType.Reference),
    Location,
    Usings.Union(declaredType.Usings),
    Kind,
    HasMetatypeAttribute || declaredType.HasMetatypeAttribute,
    IsTopLevelAccessible || declaredType.IsTopLevelAccessible,
    Properties.ToImmutableHashSet().Union(declaredType.Properties).ToImmutableArray(),
    Diagnostics.Union(declaredType.Diagnostics)
  );
}
