namespace Chickensoft.Serialization;

using System;
using System.Collections.Generic;
using System.Data;
using Chickensoft.Collections;
using Chickensoft.Introspection;

/// <summary>
/// A serializable <see cref="IBlackboard" /> implementation. Blackboards are
/// serialized as part of an owning type, rather than on their own. This allows
/// each owning type to customize its blackboard schema.
/// </summary>
public interface ISerializableBlackboard : IBlackboard {
  /// <summary>
  /// Types that should be persisted when the owning object is serialized.
  /// </summary>
  IEnumerable<Type> SavedTypes { get; }

  /// <summary>
  /// Establishes a factory that will be used for the given data type if the
  /// data was not provided during deserialization or if creating a new
  /// instance that has never been serialized.
  /// </summary>
  /// <typeparam name="TData">Type of data to persist.</typeparam>
  /// <param name="factory">Factory closure which creates the data.</param>
  void Save<TData>(Func<TData> factory) where TData : class, IIntrospective;

  /// <summary>
  /// Establishes a factory that will be used for the given data type if the
  /// data was not provided during deserialization or if creating a new
  /// instance that has never been serialized.
  /// </summary>
  /// <param name="type">Type of data to persist.</param>
  /// <param name="factory">Factory closure which creates the data.</param>
  void SaveObject(Type type, Func<object> factory);
}

/// <summary>
/// A serializable <see cref="IBlackboard" /> implementation.
/// </summary>
public class SerializableBlackboard : Blackboard, ISerializableBlackboard {
  /// <summary>
  /// Factory closures that create instances of the expected data types.
  /// </summary>
  protected readonly Dictionary<Type, Func<object>> _saveTypes =
    new();

  /// <inheritdoc />
  public IEnumerable<Type> SavedTypes => _saveTypes.Keys;

  /// <inheritdoc cref="ISerializableBlackboard.Save{TData}(Func{TData})" />
  public void Save<TData>(Func<TData> factory)
    where TData : class, IIntrospective =>
    SaveObject(typeof(TData), factory);

  /// <inheritdoc
  ///   cref="ISerializableBlackboard.SaveObject(Type, Func{object})" />
  public void SaveObject(Type type, Func<object> factory) =>
    SaveObjectData(type, factory);

  /// <summary>
  /// Instantiates and adds any missing saved data types that have not been
  /// added to the blackboard yet.
  /// </summary>
  public void InstantiateAnyMissingSavedData() {
    foreach (var type in _saveTypes.Keys) {
      if (!_blackboard.ContainsKey(type)) {
        _blackboard[type] = _saveTypes[type]();
      }
    }
  }

  /// <inheritdoc
  ///   cref="ISerializableBlackboard.SaveObject(Type, Func{object})" />
  protected virtual void SaveObjectData(Type type, Func<object> factory) {
    if (_blackboard.ContainsKey(type)) {
      throw new DuplicateNameException(
        $"Cannot mark blackboard data `{type}` to be persisted " +
        "since it would conflict with existing data added to the blackboard."
      );
    }
    // Overwrite any previously registered save factories for this type if they
    // have never been used to create an instance.
    _saveTypes[type] = factory;
  }

  /// <inheritdoc />
  protected override object GetBlackboardData(Type type) {
    // If we have data of this type on the blackboard, return it.
    if (_blackboard.TryGetValue(type, out var data)) {
      return data;
    }

    // Otherwise, create an instance of the data and add it to the main
    // blackboard since we are being asked for it.
    if (_saveTypes.ContainsKey(type)) {
      data = _saveTypes[type]();
      _blackboard[type] = data;
      return data;
    }

    // We don't have the requested data. Let the original implementation throw.
    return base.GetBlackboardData(type);
  }

  /// <inheritdoc />
  protected override void SetBlackboardData(Type type, object data) {
    if (_saveTypes.ContainsKey(type)) {
      throw new DuplicateNameException(
        $"Cannot set blackboard data `{type}` since it would conflict with " +
        "persisted data on the blackboard."
      );
    }

    base.SetBlackboardData(type, data);
  }
}
