namespace Chickensoft.Introspection.Generator.Models;

using System;
using System.Collections.Immutable;
using System.Linq;
using Chickensoft.Introspection.Generator.Utils;

public sealed record TypeReference(
  string SimpleName,
  Construction Construction,
  bool IsPartial,
  ImmutableArray<string> TypeParameters
) {
  /// <summary>
  /// Open generics portion of the type name (if generic). Otherwise, blank
  /// string.
  /// </summary>
  public string OpenGenerics => GetOpenGenerics(TypeParameters.Length);

  /// <summary>
  /// Name of the type, including any open generics portion of the name (if the
  /// type is generic).
  /// </summary>
  public string SimpleNameOpen => SimpleName + OpenGenerics;

  /// <summary>
  /// Name of the type, including any generic type parameters.
  /// </summary>
  public string SimpleNameClosed => SimpleName + GetGenerics(TypeParameters);

  /// <summary>True if the type is generic.</summary>
  public bool IsGeneric => TypeParameters.Length > 0;

  public TypeReference MergePartialDefinition(TypeReference reference) =>
    new(
      SimpleName,
      Construction,
      IsPartial || reference.IsPartial,
      TypeParameters
    );

  public string CodeString => GetConstructionCodeString(
    IsPartial,
    Construction,
    SimpleNameClosed
  );

  public bool Equals(TypeReference? other) =>
    other is not null &&
    SimpleName == other.SimpleName &&
    Construction == other.Construction &&
    IsPartial == other.IsPartial &&
    TypeParameters.SequenceEqual(other.TypeParameters);

  public override int GetHashCode() => HashCode.Combine(
    SimpleName,
    Construction,
    IsPartial,
    TypeParameters
  );

  public static string GetGenerics(ImmutableArray<string> typeParameters) =>
    typeParameters.Length > 0
      ? $"<{string.Join(", ", typeParameters)}>"
      : string.Empty;

  public static string GetOpenGenerics(int numTypeParameters) =>
    numTypeParameters > 0
      ? $"<{new string(',', numTypeParameters - 1)}>"
      : string.Empty;

  /// <summary>
  /// Returns the code needed to declare the type.
  /// </summary>
  /// <param name="isPartial">True if the type is partial.</param>
  /// <param name="construction">Type's construction.</param>
  /// <param name="name">Name of the type, including any generics.</param>
  /// <returns>Code string.</returns>
  public static string GetConstructionCodeString(
    bool isPartial,
    Construction construction,
    string name
  ) {
    var partial = isPartial ? "partial " : string.Empty;
    var code = construction switch {
      Construction.StaticClass => $"static {partial}class ",
      Construction.Class => $"{partial}class ",
      Construction.RecordStruct => $"{partial}record struct ",
      Construction.RecordClass => $"{partial}record class ",
      Construction.Interface => $"{partial}interface ",
      Construction.Struct => $"{partial}struct ",
      _ => throw new ArgumentException(
        $"Unsupported construction type: {construction}", nameof(construction)
      )
    };

    return code + name;
  }
}
