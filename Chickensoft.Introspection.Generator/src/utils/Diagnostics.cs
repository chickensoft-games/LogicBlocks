namespace Chickensoft.LogicBlocks.Generator.Utils;

using Microsoft.CodeAnalysis;

public static class Diagnostics {
  public const string ERR_PREFIX = "LOGIC_BLOCKS";
  public const string ERR_CATEGORY = "Chickensoft.LogicBlocks.Generator";

  public static Diagnostic InvalidIntrospectiveType(
    SyntaxNode node, string name
  )
  => Diagnostic.Create(
    new(
      $"{ERR_PREFIX}_000",
      $"Invalid [{Constants.INTROSPECTIVE_ATTRIBUTE_NAME}] Usage",
      messageFormat:
        "Invalid use of [" + Constants.INTROSPECTIVE_ATTRIBUTE_NAME + "] " +
        "on `{0}`. Please make sure the [" +
        Constants.INTROSPECTIVE_ATTRIBUTE_NAME + "] " +
        "attribute is placed on a non-generic, partial class or record " +
        "class that is visible from the root namespace.",
      category: ERR_CATEGORY,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true
    ),
    node.GetLocation(),
    name
  );
}
