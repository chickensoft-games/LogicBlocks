
namespace Chickensoft.LogicBlocks.Generator.Types.Models;

/// <summary>
/// Represents a declared type.
/// </summary>
/// <param name="Location">Location of the type in the source code.</param>
/// <param name="Kind">Kind of the type.</param>
/// <param name="IsVisible">True if the public or internal visibility modifier
/// was seen on the type.</param>
/// <param name="NumTypeParameters">Number of type parameters the type has.
/// </param>
/// <param name="Name">Name of the type itself.</param>
public record DeclaredTypeInfo(
  TypeLocation Location,
  DeclaredTypeKind Kind,
  bool IsVisible,
  int NumTypeParameters,
  string Name
) {
  /// <summary>
  /// Fully qualified name, as determined based on syntax nodes only.
  /// </summary>
  public string FullName => Location.Prefix + Name + OpenGenerics;

  public string OpenGenerics => NumTypeParameters > 0
    ? $"<{new string(',', NumTypeParameters - 1)}>"
    : string.Empty;

  /// <summary>
  /// Combine with another declared type that represents the exact same type.
  /// </summary>
  /// <param name="declaredType">Declared type representing the same type.
  /// </param>
  /// <returns>Updated representation of the declared type.</returns>
  public DeclaredTypeInfo Combine(DeclaredTypeInfo declaredType) => new(
    Location,
    Kind,
    IsVisible || declaredType.IsVisible,
    NumTypeParameters,
    Name
  );
}
