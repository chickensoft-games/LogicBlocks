namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Concurrent;
using Chickensoft.Serialization;

/// <summary>
/// <inheritdoc cref="LogicBlockBase" path="/summary" />
/// </summary>
public interface ILogicBlockBase {
  /// <summary>
  /// Current state of the logic block, if any. Reading this will not start
  /// the logic block and can return null.
  /// </summary>
  object? ValueAsObject { get; }

  /// <summary>State that will be restored when started, if any.</summary>
  object? RestoredState { get; }

  /// <summary>Internal blackboard of the logic block.</summary>
  SerializableBlackboard Blackboard { get; }

  /// <summary>
  /// Restore the state from a given object. Only works if the current
  /// state has not been initialized and the <paramref name="state"/> is
  /// of the correct type.
  /// </summary>
  /// <param name="state">State to restore.</param>
  void RestoreState(object state);
}

/// <summary>
/// Common, non-generic base type for all logic blocks. This exists to allow
/// all logic blocks in a codebase to be identified by inspecting the derived
/// types computed from the generated type registry that the logic blocks
/// generator produces.
/// </summary>
public abstract class LogicBlockBase : ILogicBlockBase {
  /// <inheritdoc />
  public abstract object? ValueAsObject { get; }

  /// <inheritdoc />
  public object? RestoredState { get; set; }

  /// <inheritdoc />
  public SerializableBlackboard Blackboard { get; } = new();

  /// <inheritdoc />
  public abstract void RestoreState(object state);

  /// <summary>
  /// Used by the logic blocks serializer to see if a given logic block state
  /// has diverged from an unaltered copy of the state that's stored here —
  /// one reference state for every type (not instance) of a logic block state.
  /// </summary>
  internal static ConcurrentDictionary<Type, object> ReferenceStates { get; } =
    new();
}
