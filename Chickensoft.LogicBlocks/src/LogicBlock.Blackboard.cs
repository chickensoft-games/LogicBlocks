namespace Chickensoft.LogicBlocks;

using System.Runtime.CompilerServices;
using Collections;

public partial class LogicBlock : IBlackboard
{
  #region IReadOnlyBlackboard

  /// <inheritdoc />
  public IReadOnlySet<Type> Types => Blackboard.Types;

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public TData Get<TData>() where TData : class => Blackboard.Get<TData>();

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object GetObject(Type type) => Blackboard.GetObject(type);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Has<TData>() where TData : class => Blackboard.Has<TData>();

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool HasObject(Type type) => Blackboard.HasObject(type);

  #endregion IReadOnlyBlackboard

  #region IBlackboard

  /// <inheritdoc cref="IBlackboard.Set{TData}(TData)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Set<TData>(TData data) where TData : class =>
    Blackboard.Set(data);

  /// <inheritdoc cref="IBlackboard.SetObject(Type, object)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetObject(Type type, object data) =>
    Blackboard.SetObject(type, data);

  /// <inheritdoc cref="IBlackboard.Overwrite{TData}(TData)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Overwrite<TData>(TData data) where TData : class =>
    Blackboard.Overwrite(data);

  /// <inheritdoc cref="IBlackboard.OverwriteObject(Type, object)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void OverwriteObject(Type type, object data) =>
    Blackboard.OverwriteObject(type, data);

  #endregion IBlackboard
}
