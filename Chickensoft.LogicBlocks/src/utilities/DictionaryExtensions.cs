namespace Chickensoft.LogicBlocks.Extensions;

using System.Collections.Generic;

/// <summary>
/// Dictionary extensions.
/// </summary>
internal static class DictionaryExtensions {
  /// <summary>
  /// Adds a value to the dictionary if there is no value already present for
  /// the given key.
  /// </summary>
  /// <param name="dictionary">Dictionary receiver.</param>
  /// <param name="key">Dictionary key.</param>
  /// <param name="value">Dictionary value to add.</param>
  /// <typeparam name="TKey">Key type.</typeparam>
  /// <typeparam name="TValue">Value type.</typeparam>
  public static void AddIfNotPresent<TKey, TValue>(
    this Dictionary<TKey, TValue> dictionary,
    TKey key,
    TValue value
  ) where TKey : notnull {
    if (!dictionary.ContainsKey(key)) {
      dictionary[key] = value;
    }
  }
}
