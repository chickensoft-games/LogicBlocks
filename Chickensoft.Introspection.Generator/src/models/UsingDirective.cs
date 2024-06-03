namespace Chickensoft.Introspection.Generator.Models;

using Chickensoft.Introspection.Generator.Utils;

/// <summary>
/// Using directive. In C# 12, a using can be global, static, and an alias all
/// at the same time.
/// </summary>
/// <param name="Alias">If the using is an alias expression, this is the alias
/// name.</param>
/// <param name="Name">The namespace to import.</param>
/// <param name="IsGlobal">True if this is a global using statement.</param>
/// <param name="IsStatic">True if this is a static using alias.</param>
/// <param name="IsAlias">True if this is a using alias.</param>
public sealed record UsingDirective(
  string? Alias,
  string Name,
  bool IsGlobal,
  bool IsStatic,
  bool IsAlias
) {
  public string CodeString => IsGlobal
    ? $"global using {Name};"
    : IsStatic
      ? $"using static {Name};"
      : IsAlias
        ? $"using {Alias} = {Name};"
        : $"using {Name};";

  public bool Equals(UsingDirective? other) =>
    other is not null &&
    Alias == other.Alias &&
    Name == other.Name &&
    IsGlobal == other.IsGlobal &&
    IsStatic == other.IsStatic &&
    IsAlias == other.IsAlias;

  public override int GetHashCode() => HashCode.Combine(
    Alias, Name, IsGlobal, IsStatic, IsAlias
  );
}
