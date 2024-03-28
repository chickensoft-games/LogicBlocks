namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// A read-only blackboard. A blackboard is a table of data. Data is accessed by
/// its type and shared between logic block states.
/// </summary>
public interface IReadOnlyBlackboard {
  /// <summary>
  /// Gets data from the blackboard by its compile-time type.
  /// </summary>
  /// <typeparam name="TData">The type of data to retrieve.</typeparam>
  /// <exception cref="LogicBlockException" />
  TData Get<TData>() where TData : class;
  /// <summary>
  /// Gets data from the blackboard by its runtime type.
  /// </summary>
  /// <param name="type">Type of the data to retrieve.</param>
  /// <exception cref="LogicBlockException" />
  object GetObject(Type type);
  /// <summary>
  /// Determines if the logic block has data of the given type.
  /// </summary>
  /// <typeparam name="TData">The type of data to look for.</typeparam>
  /// <returns>True if the blackboard contains data for that type, false
  /// otherwise.</returns>
  bool Has<TData>() where TData : class;
  /// <summary>
  /// Determines if the logic block has data of the given type.
  /// </summary>
  /// <param name="type">The type of data to look for.</param>
  /// <returns>True if the blackboard contains data for that type, false
  /// otherwise.</returns>
  bool HasObject(Type type);
}

/// <summary>
/// A blackboard is a table of data. Data is accessed by its type and shared
/// between logic block states.
/// </summary>
public interface IBlackboard : IReadOnlyBlackboard {
  /// <summary>
  /// Adds data to the blackboard so that it can be looked up by its
  /// compile-time type.
  /// <br />
  /// Data is retrieved by its type, so do not add more than one piece of data
  /// with the same type.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <typeparam name="TData">Type of the data to add.</typeparam>
  /// <exception cref="LogicBlockException">Thrown if data of the provided type
  /// has already been added.</exception>
  void Set<TData>(TData data) where TData : class;
  /// <summary>
  /// Adds data to the blackboard so that it can be looked up by its runtime
  /// type.
  /// <br />
  /// Data is retrieved by its type, so do not add more than one piece of data
  /// with the same type.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <exception cref="LogicBlockException">Thrown if data of the provided type
  /// has already been added.</exception>
  void SetObject(object data);
  /// <summary>
  /// Adds new data or overwrites existing data in the blackboard, based on
  /// its compile-time type.
  /// <br />
  /// Data is retrieved by its type, so this will overwrite any existing data
  /// of the given type, unlike <see cref="Set{TData}(TData)" />.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <typeparam name="TData">Type of the data to add or overwrite.</typeparam>
  void Overwrite<TData>(TData data) where TData : class;
  /// <summary>
  /// Adds new data or overwrites existing data in the blackboard, based on
  /// its runtime type.
  /// <br />
  /// Data is retrieved by its type, so this will overwrite any existing data
  /// of the given type, unlike <see cref="Set{TData}(TData)" />.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  void OverwriteObject(object data);
}

/// <summary><inheritdoc cref="IBlackboard" /></summary>
public class Blackboard : IBlackboard {
  private readonly Dictionary<Type, object> _blackboard = new();

  /// <summary>
  /// Creates a new blackboard. <inheritdoc cref="Blackboard" />
  /// </summary>
  public Blackboard() { }

  #region IReadOnlyBlackboard
  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public TData Get<TData>() where TData : class {
    var type = typeof(TData);
    return (TData)GetBlackboardData(type);
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object GetObject(Type type) => GetBlackboardData(type);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Has<TData>() where TData : class => HasObject(typeof(TData));

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool HasObject(Type type) => _blackboard.ContainsKey(type);
  #endregion IReadOnlyBlackboard

  #region Blackboard
  /// <summary>
  /// Adds data to the blackboard so that it can be looked up by its
  /// compile-time type.
  /// <br />
  /// Data is retrieved by its type, so do not add more than one piece of data
  /// with the same type.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <typeparam name="TData">Type of the data to add.</typeparam>
  /// <exception cref="ArgumentException">Thrown if data of the provided type
  /// has already been added.</exception>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Set<TData>(TData data) where TData : class {
    var type = typeof(TData);
    SetBlackboardData(type, data);
  }

  /// <summary>
  /// Adds data to the blackboard so that it can be looked up by its runtime
  /// type.
  /// <br />
  /// Data is retrieved by its type, so do not add more than one piece of data
  /// with the same type.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <exception cref="LogicBlockException">Thrown if data of the provided type
  /// has already been added.</exception>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetObject(object data) {
    var type = data.GetType();
    SetBlackboardData(type, data);
  }

  /// <summary>
  /// Adds new data or overwrites existing data in the blackboard, based on
  /// its compile-time type.
  /// <br />
  /// Data is retrieved by its type, so this will overwrite any existing data
  /// of the given type, unlike <see cref="Set{TData}(TData)" />.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <typeparam name="TData">Type of the data to add or overwrite.</typeparam>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Overwrite<TData>(TData data) where TData : class =>
    _blackboard[typeof(TData)] = data;

  /// <summary>
  /// Adds new data or overwrites existing data in the blackboard, based on
  /// its runtime type.
  /// <br />
  /// Data is retrieved by its type, so this will overwrite any existing data
  /// of the given type, unlike <see cref="Set{TData}(TData)" />.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void OverwriteObject(object data) =>
    _blackboard[data.GetType()] = data;
  #endregion Blackboard

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private object GetBlackboardData(Type type) =>
    _blackboard.TryGetValue(type, out var data)
      ? data
      : throw new LogicBlockException(
        $"Data of type {type} not found in the blackboard."
      );

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetBlackboardData(Type type, object data) {
    if (!_blackboard.TryAdd(type, data)) {
      throw new LogicBlockException(
        $"Data of type {type} already exists in the blackboard."
      );
    }
  }
}
