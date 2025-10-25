namespace Chickensoft.LogicBlocks.Analyzers.Utils;

using Microsoft.CodeAnalysis;

public static class Diagnostics
{
  public const string ERR_PREFIX = "LOGIC_BLOCKS";
  public const string ERR_CATEGORY = "Chickensoft.LogicBlocks.Analyzers";

  public static DiagnosticDescriptor MissingLogicBlockAttributeDescriptor
  {
    get;
  } = new(
    $"{ERR_PREFIX}_001",
    $"Missing [{Constants.LOGIC_BLOCK_ATTRIBUTE_NAME}]",
    messageFormat:
      $"Missing [{Constants.LOGIC_BLOCK_ATTRIBUTE_NAME}] on logic block " +
      "implementation `{0}`",
    ERR_CATEGORY,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );

  public static Diagnostic MissingLogicBlockAttribute(
    Location location, string name
  ) => Diagnostic.Create(MissingLogicBlockAttributeDescriptor, location, name);
}
