namespace Chickensoft.LogicBlocks;

using Chickensoft.Serialization;

/// <summary>
/// Common, non-generic base type for all logic blocks. This exists to allow
/// all logic blocks in a codebase to be identified by inspecting the derived
/// types computed from the generated type registry that the logic blocks
/// generator produces.
/// </summary>
public abstract class LogicBlockBase {
  /// <summary>
  /// Current state of the logic block, if any. Logic blocks that haven't been
  /// started yet will not have a state.
  /// </summary>
  public abstract object ValueAsPlainObject { get; }

  /// <summary>
  /// Restore the state from a given object. Only works if the current
  /// state has not been initialized and the <paramref name="state"/> is
  /// of the correct type.
  /// </summary>
  /// <param name="state">State to restore.</param>
  public abstract void RestoreState(object state);

  /// <summary>Internal blackboard of the logic block.</summary>
  internal readonly SerializableBlackboard _blackboard = new();
}
