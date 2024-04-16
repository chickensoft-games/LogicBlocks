namespace Chickensoft.LogicBlocks.Generator.Types;

using Microsoft.CodeAnalysis;

public static class Diagnostics {
  public const string ERR_PREFIX = "LOGIC_BLOCKS_TYPES";
  public const string ERR_CATEGORY = "Chickensoft.LogicBlocks.Generator.Types";

  public static Diagnostic InvalidMetatype(SyntaxNode node, string name)
  => Diagnostic.Create(
    new(
      $"{ERR_PREFIX}_000",
      "Invalid MetatypeAttribute Usage",
      messageFormat:
        "Invalid use of the MetatypeAttribute on `{0}`. Please make sure the " +
        "attribute is placed on a non-generic partial class or record " +
        "class that is visible from the root namespace.",
      category: ERR_CATEGORY,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true
    ),
    node.GetLocation(),
    name
  );
}
