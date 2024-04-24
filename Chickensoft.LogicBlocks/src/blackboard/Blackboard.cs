namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
  /// Adds data to the blackboard and associates it with a specific type.
  /// <br />
  /// Data is retrieved by its type, so do not add more than one piece of data
  /// with the same type.
  /// </summary>
  /// <param name="type">Type of the data.</param>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <exception cref="LogicBlockException">Thrown if data of the provided type
  /// has already been added.</exception>
  void SetObject(Type type, object data);
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
  /// Adds new data or overwrites existing data in the blackboard and associates
  /// it with a specific type.
  /// <br />
  /// Data is retrieved by its type, so this will overwrite any existing data
  /// of the given type, unlike <see cref="Set{TData}(TData)" />.
  /// </summary>
  /// <param name="type">Type of the data.</param>
  /// <param name="data">Data to write to the blackboard.</param>
  void OverwriteObject(Type type, object data);
}

/// <summary><inheritdoc cref="IBlackboard" /></summary>
public class Blackboard : IBlackboard {
  /// <summary>Blackboard data storage.</summary>
  protected readonly Dictionary<Type, object> _blackboard = new();

  /// <summary>
  /// Creates a new blackboard. <inheritdoc cref="Blackboard" />
  /// </summary>
  public Blackboard() { }

  #region IReadOnlyBlackboard
  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public virtual TData Get<TData>() where TData : class {
    var type = typeof(TData);
    return (TData)GetBlackboardData(type);
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public virtual object GetObject(Type type) => GetBlackboardData(type);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Has<TData>() where TData : class => HasObject(typeof(TData));

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool HasObject(Type type) => _blackboard.ContainsKey(type);
  #endregion IReadOnlyBlackboard

  #region Blackboard
  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Set<TData>(TData data) where TData : class {
    var type = typeof(TData);
    SetBlackboardData(type, data);
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetObject(Type type, object data) =>
    SetBlackboardData(type, data);

  /// <inheritdoc />
  public void Overwrite<TData>(TData data) where TData : class =>
    _blackboard[typeof(TData)] = data;

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void OverwriteObject(Type type, object data) =>
    _blackboard[type] = data;
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
