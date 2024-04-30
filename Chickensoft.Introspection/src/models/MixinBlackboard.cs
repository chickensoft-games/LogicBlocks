namespace Chickensoft.Introspection;

using Chickensoft.Collections;

/// <summary>
/// Represents a simple table of data that can be used to store a single
/// object instance by its type.
/// </summary>
public class MixinBlackboard : Blackboard {
  /// <inheritdoc />
  // Equal to everything to avoid being a factor when a child of a record type.
  public override bool Equals(object obj) => true;

  /// <inheritdoc />
  public override int GetHashCode() => base.GetHashCode();
}
