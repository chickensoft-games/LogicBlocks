namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Chickensoft.Serialization;

/// <summary>
/// Common, non-generic base type for all logic blocks. This exists to allow
/// all logic blocks in a codebase to be identified by inspecting the derived
/// types computed from the generated type registry that the logic blocks
/// generator produces.
/// </summary>
public abstract class LogicBlockBase {
  /// <summary>
  /// Current state of the logic block, if any. Reading this will not start
  /// the logic block and can return null.
  /// </summary>
  internal abstract object? ValueAsObject { get; }

  /// <summary>
  /// Used by the logic blocks serializer to see if a given logic block state
  /// has diverged from an unaltered copy of the state that's stored here —
  /// one reference state for every logic block state.
  /// </summary>
  internal static ConcurrentDictionary<Type, object> ReferenceStates { get; } =
    new();

  /// <summary>
  /// Restore the state from a given object. Only works if the current
  /// state has not been initialized and the <paramref name="state"/> is
  /// of the correct type.
  /// </summary>
  /// <param name="state">State to restore.</param>
  public abstract void RestoreState(object state);

  /// <summary>Internal blackboard of the logic block.</summary>
  internal readonly SerializableBlackboard _blackboard = new();

  internal object? _restoredState;

  /// <summary>
  /// Determines if two logic block states are equivalent. Logic block states
  /// are equivalent if they are the same reference or are equal according to
  /// the default equality comparer.
  /// </summary>
  /// <param name="a">First state.</param>
  /// <param name="b">Second state.</param>
  /// <returns>True if the states are equivalent.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsEquivalent(object? a, object? b) =>
    ReferenceEquals(a, b) || (
      a is null &&
      b is null
    ) || (
      a is not null &&
      b is not null &&
      EqualityComparer<object>.Default.Equals(a, b)
    );
}
