namespace Chickensoft.Serialization;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>Serialization utilities.</summary>
public static class SerializationUtilities {
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
