namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a simple table of data that can be used to store a single
/// object instance by its type.
/// </summary>
public class DataTable {
  private readonly Dictionary<Type, object> _data = new();

  /// <summary>
  /// Gets the object instance of the specified type from the data table.
  /// </summary>
  /// <typeparam name="T">The type of the object instance to get.</typeparam>
  /// <returns>The object instance of the specified type.</returns>
  /// <exception cref="KeyNotFoundException" />
  public T Get<T>() where T : class {
    var type = typeof(T);

    return _data.ContainsKey(type)
      ? (T)_data[type]
      : throw new KeyNotFoundException(
        $"Type `{type}` not found in the data table."
      );
  }

  /// <summary>
  /// Sets the object instance of the specified type in the data table.
  /// </summary>
  /// <typeparam name="T">The type of the object instance to set.</typeparam>
  /// <param name="value">The object instance to set.</param>
  /// <exception cref="ArgumentNullException" />
  public void Set<T>(T value) where T : class => _data[typeof(T)] = value;
}
