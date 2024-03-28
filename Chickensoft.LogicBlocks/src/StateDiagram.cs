namespace Chickensoft.LogicBlocks.Generator;

using System;

/// <summary>
/// State diagram attribute. Placing this on a LogicBlock implementation will
/// enable the LogicBlocks diagram generator to create a high-level UML state
/// diagram.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StateDiagramAttribute : Attribute {
  /// <summary>
  /// State type. This can be an abstract record, but it can't be an interface.
  /// </summary>
  public Type StateType { get; }

  /// <summary>
  /// State diagram attribute. Placing this on a LogicBlock implementation will
  /// enable the LogicBlocks diagram generator to create a high-level UML state
  /// diagram.
  /// </summary>
  /// <param name="stateType">State type. This can be an abstract record,
  /// but it can't be an interface.</param>
  public StateDiagramAttribute(Type stateType) {
    StateType = stateType;
  }
}
