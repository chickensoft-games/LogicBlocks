namespace Chickensoft.Introspection.Generator.Models;

/// <summary>
/// Using directive. In C# 12, a using can be global, static, and an alias all
/// at the same time.
/// </summary>
/// <param name="Alias">If the using is an alias expression, this is the alias
/// name.</param>
/// <param name="TypeName">The type to use.</param>
/// <param name="IsGlobal">True if this is a global using statement.</param>
/// <param name="IsStatic">True if this is a static using alias.</param>
/// <param name="IsAlias">True if this is a using alias.</param>
public readonly record struct UsingDirective(
  string? Alias,
  string TypeName,
  bool IsGlobal,
  bool IsStatic,
  bool IsAlias
) {
  public string CodeString => IsGlobal
    ? $"global using {TypeName};"
    : IsStatic
      ? $"using static {TypeName};"
      : IsAlias
        ? $"using {Alias} = {TypeName};"
        : $"using {TypeName};";
}
