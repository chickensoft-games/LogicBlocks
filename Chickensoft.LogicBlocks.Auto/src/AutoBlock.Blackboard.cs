namespace Chickensoft.LogicBlocks.Auto;

using System;
using System.Collections.Generic;
using Introspection;
using Serialization;

/// <inheritdoc cref="AutoBlock"/>
public partial class AutoBlock : ISerializableBlackboard
{
  #region ISerializableBlackboard

  /// <inheritdoc cref="ISerializableBlackboard.SavedTypes" />
  public IEnumerable<Type> SavedTypes => _serializableBlackboard.SavedTypes;

  /// <inheritdoc cref="ISerializableBlackboard.TypesToSave" />
  public IEnumerable<Type> TypesToSave => _serializableBlackboard.TypesToSave;

  /// <inheritdoc cref="ISerializableBlackboard.Save{TData}(Func{TData})" />
  public void Save<TData>(Func<TData> factory)
    where TData : class, IIdentifiable => _serializableBlackboard.Save(factory);

  /// <inheritdoc
  /// cref="ISerializableBlackboard.SaveObject(Type, Func{object}, object?)" />
  public void SaveObject(
    Type type, Func<object> factory, object? referenceValue
  ) => _serializableBlackboard.SaveObject(type, factory, referenceValue);

  #endregion ISerializableBlackboard
}
