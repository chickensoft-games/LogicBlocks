namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// Logic block attribute. Place this on a class that extends
/// <see cref="LogicBlock{TState}" /> to enable the LogicBlocks generators to
/// generate serialization utilities and/or a state diagram of the hierarchical
/// state machine that the logic block represents.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LogicBlockAttribute : Attribute {
  /// <summary>
  /// State type. If the state type is an interface, this should be the single
  /// base state record from which all other states are derived, not the
  /// interface.
  /// </summary>
  public Type StateType { get; }

  /// <summary>
  /// Whether or not a diagram should be generated for this logic block. The
  /// default is false.
  /// </summary>
  public bool Diagram { get; set; } = false;

  /// <summary>
  /// Logic block attribute. Place this on a class that extends
  /// <see cref="LogicBlock{TState}" /> to enable the LogicBlocks generators to
  /// generate serialization utilities and/or a state diagram of the
  /// hierarchical state machine that the logic block represents.
  /// </summary>
  /// <param name="stateType">State type. If the state is an interface,
  /// specify the single base state record from which all other states are
  /// derived, not the interface.</param>
  public LogicBlockAttribute(Type stateType) {
    StateType = stateType;
  }
}
