
namespace Chickensoft.LogicBlocks.Generator.Types.Models;

using System.Collections.Immutable;

public record TypeReference(
  string Name,
  Construction Construction,
  bool IsPartial,
  ImmutableArray<string> TypeParameters
) {
  /// <summary>
  /// Open generics portion of the type name (if generic). Otherwise, blank
  /// string.
  /// </summary>
  public string OpenGenerics => GetOpenGenerics(
    TypeParameters.Length
  );

  /// <summary>
  /// Name of the type, including any open generics portion of the name (if the
  /// type is generic).
  /// </summary>
  public string NameWithOpenGenerics => Name + OpenGenerics;

  /// <summary>
  /// Name of the type, including any generic type parameters.
  /// </summary>
  public string NameWithGenerics => Name + (
    TypeParameters.Length > 0
      ? "<" + string.Join(", ", TypeParameters) + ">"
      : ""
  );

  /// <summary>True if the type is generic.</summary>
  public bool IsGeneric => TypeParameters.Length > 0;

  public TypeReference MergePartialDefinition(TypeReference reference) =>
    new(
      Name,
      Construction,
      IsPartial || reference.IsPartial,
      TypeParameters
    );

  public string CodeString => GetConstructionCodeString(
    IsPartial,
    Construction,
    NameWithGenerics
  );

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
      _ => "class"
    };

    return code + name;
  }
}
