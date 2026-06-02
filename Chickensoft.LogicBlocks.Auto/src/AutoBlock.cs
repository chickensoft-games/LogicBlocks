namespace Chickensoft.LogicBlocks.Auto;

using System;
using Introspection;
using Serialization;

/// <summary>
/// A logic block that automatically pre-allocates states and supports
/// serialization and deserialization.
/// </summary>
public interface IAutoLogicBlock : ILogicBlock
{
  /// <summary>Serializes the logic block's current state to save data.</summary>
  public ILogicBlockSaveData Save();
}

/// <summary>Represents serialized save data for a logic block.</summary>
public interface ILogicBlockSaveData : IIdentifiable
{
  /// <summary>The serialized logic block data payload.</summary>
  LogicBlockData Data { get; }
}

public abstract partial class AutoBlock : LogicBlock, IAutoLogicBlock
{
  internal readonly SerializableBlackboard _serializableBlackboard;

  /// <summary>
  /// Creates a new <see cref="AutoBlock"/> with the given history limit and
  /// pre-allocated state types.
  /// </summary>
  /// <param name="maxHistory">Maximum number of history entries to retain.</param>
  /// <param name="stateTypes">State types to pre-allocate.</param>
  public AutoBlock(
    int? maxHistory = MAX_HISTORY_DEFAULT,
    params Type[] stateTypes
  ) : base(new SerializableBlackboard(), maxHistory)
  {
    _serializableBlackboard = (SerializableBlackboard)_blackboard!;

    for (var i = 0; i < stateTypes.Length; i++)
    {
      Preallocate(stateTypes[i]);
    }
  }

  /// <summary>Called after the logic block is loaded from save data.</summary>
  public virtual void OnLoad() { }

  /// <summary>
  /// Returns the save data for the logic block. Override this method to
  /// provide a concrete save data type.
  /// </summary>
  /// <param name="data">The serialized logic block data.</param>
  public virtual ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    throw new LogicBlockException(
    $"GetSaveData() not implemented for {GetType().Name}, please ensure the " +
    $"method GetSaveData() is overridden like so:\n" +
    $"`public override ILogicBlockSaveData GetSaveData(LogicBlockData data) => " +
    $"new {GetType().Name}SaveData {{ Data = data }};`"
  );

  /// <inheritdoc cref="IAutoLogicBlock.Save"/>
  public ILogicBlockSaveData Save() => GetSaveData(GetData());

  internal override void Loaded(
  )
  {
    _serializableBlackboard.InstantiateAnyMissingSavedData();

    OnLoad();
  }
}
