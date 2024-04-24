namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using Chickensoft.Introspection;

/// <summary>
/// A serializable <see cref="IBlackboard" /> implementation.
/// </summary>
public interface ISerializableBlackboard : IBlackboard {
  /// <summary>
  /// Establishes a factory that will be used for the given data type if the
  /// data was not provided during deserialization or if creating a new
  /// instance that has never been serialized.
  /// </summary>
  /// <typeparam name="TData">Type of data to persist.</typeparam>
  /// <param name="factory">Factory closure which creates the data.</param>
  void Save<TData>(Func<TData> factory) where TData : class, IIntrospective;

  /// <summary>
  /// Registers <see cref="JsonPropertyInfo"/> on the provided
  /// <see cref="JsonTypeInfo"/> to describe the persistent blackboard data.
  /// </summary>
  /// <param name="typeInfo">The <see cref="JsonTypeInfo" /> to register
  /// persistent <see cref="JsonPropertyInfo"/> descriptions on.</param>
  /// <param name="registry">The <see cref="ITypeRegistry" /> to use for
  /// resolving generated type information.</param>
  /// <typeparam name="TStateType">State type of the owning logic block.
  /// </typeparam>
  void RegisterJsonPropertyInfo<TStateType>(
    JsonTypeInfo typeInfo, ITypeRegistry registry
  ) where TStateType : class, IStateLogic<TStateType>;
}

/// <summary>
/// A serializable <see cref="IBlackboard" /> implementation.
/// </summary>
public class SerializableBlackboard : Blackboard {
  /// <summary>
  /// Factory closures that create instances of the expected data types.
  /// </summary>
  protected readonly Dictionary<Type, Func<object>> _serializedTypes =
    new();

  /// <inheritdoc cref="ISerializableBlackboard.Save{TData}(Func{TData})" />
  public void Save<TData>(Func<TData> factory)
    where TData : class, IIntrospective =>
      _serializedTypes[typeof(TData)] = () => factory();

  /// <inheritdoc />
  public override TData Get<TData>() {
    // If the data is already in the blackboard, return it.
    if (Has<TData>()) {
      return base.Get<TData>();
    }

    // We've been asked to fetch data that was expected to be deserialized
    // (but wasn't), so we can create a new instance of the data type.
    if (_serializedTypes.TryGetValue(typeof(TData), out var factory)) {
      var data = (TData)factory();
      Set(data);
    }

    return base.Get<TData>();
  }

  /// <inheritdoc cref="ISerializableBlackboard.RegisterJsonPropertyInfo
  /// (JsonTypeInfo, ITypeRegistry)" />
  public void RegisterJsonPropertyInfo<TStateType>(
    JsonTypeInfo typeInfo, ITypeRegistry registry
  ) where TStateType : class, IStateLogic<TStateType> {
    foreach (var propertyType in _serializedTypes.Keys) {
      // Serialized types are expected to be logic models.
      var metatype = registry.Metatypes[propertyType];

      var propertyInfo = typeInfo.CreateJsonPropertyInfo(
        propertyType, metatype.Id
      );

      // Blackboard properties are always optional, since we can create an
      // instance if one wasn't available during deserialization. This increases
      // the flexibility of the blackboard during serialization, allowing for
      // faster iteration.
      propertyInfo.IsRequired = false;

      propertyInfo.Get = (object obj) =>
        ((LogicBlock<TStateType>)obj).GetObject(propertyType);

      propertyInfo.Set = (object obj, object? value) => {
        if (value is null) { return; }
        ((LogicBlock<TStateType>)obj).OverwriteObject(propertyType, value);
      };

      typeInfo.Properties.Add(propertyInfo);
    }
  }
}
