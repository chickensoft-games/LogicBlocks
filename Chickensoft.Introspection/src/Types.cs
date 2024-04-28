namespace Chickensoft.Introspection;

/// <summary>
/// LogicBlock type hierarchy lookup system — find and cache types by their
/// base type or ancestor without reflection, using the LogicBlocks Type
/// Generator output.
/// </summary>
public static class Types {
  /// <summary>Shared type graph instance.</summary>
  public static ITypeGraph Graph => InternalGraph;

  internal static TypeGraph InternalGraph { get; } = new TypeGraph();
}
