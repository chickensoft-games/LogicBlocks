namespace Chickensoft.LogicBlocks;

using System;

/// <summary>Update lifecycle callback.</summary>
internal readonly struct UpdateCallback
{
  /// <summary>Update callback function.</summary>
  public Action<object?> Callback { get; }
  /// <summary>Predicate that determines if the given instance is the expected
  /// state type.</summary>
  public Type Type { get; }

  /// <summary>Create a new update callback.</summary>
  /// <param name="callback">Callback to invoke.</param>
  /// <param name="type">Expected type of the object.</param>
  public UpdateCallback(Action<object?> callback, Type type)
  {
    Callback = callback;
    Type = type;
  }

  /// <summary>
  /// Returns true if the given object is of the expected type (or a subtype).
  /// If object represents a type, it will be checked against the expected type.
  /// Works in reflection-free mode:
  /// https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/reflection-free-mode.md
  /// </summary>
  /// <param name="obj">Possible object instance.</param>
  /// <returns>True if the given object is of the expected type (or a subtype).
  /// </returns>
  public bool IsType(object? obj) => obj is Type type
    ? Type.IsAssignableFrom(type)
    : obj is not null && Type.IsAssignableFrom(obj.GetType());
}
