namespace Chickensoft.LogicBlocks;

using System;

/// <summary>Update lifecycle callback.</summary>
internal readonly struct UpdateCallback {
  /// <summary>Update callback function.</summary>
  public Action<object?> Callback { get; }
  /// <summary>Predicate that determines if the given instance is the expected
  /// state type.</summary>
  public Func<object?, bool> IsType { get; }

  /// <summary>Create a new update callback.</summary>
  /// <param name="callback">Callback to invoke.</param>
  /// <param name="isType">Predicate that determines whether or not an object
  /// is the type the update callback handles.</param>
  public UpdateCallback(Action<object?> callback, Func<object?, bool> isType) {
    Callback = callback;
    IsType = isType;
  }
}
