namespace Chickensoft.LogicBlocks.Tutorial;

using Auto;
using Chickensoft.Introspection;
using Serialization;

public interface ISerializableLogicBlock : IAutoLogicBlock;

[Meta, Id("serializable_logic")]
public partial class SerializableLogicBlock :
AutoBlock, ISerializableLogicBlock
{
  public SerializableLogicBlock()
  {
    Preallocate<TimerState>();
  }

  public override ILogicBlockSaveData Serialize(LogicBlockData data) =>
    new SerializableLogicBlockSaveData { Data = data };
}

[Meta, Id("serializable_logic_block_save_data")]
public partial class SerializableLogicBlockSaveData : ILogicBlockSaveData
{
  [Save("data")]
  public required LogicBlockData Data { get; init; }
}

[Meta]
public abstract partial record TimerState : LogicBlockState
{
  [Meta, Id("serializable_logic_state_off")]
  public partial record PoweredOff : TimerState;

  [Meta, Id("serializable_logic_state_on")]
  public partial record PoweredOn : TimerState;

  [Meta, Id("serializable_logic_versioned_state")]
  public abstract partial record VersionedState : TimerState;

  [Meta, Version(1)]
  public partial record Version1 : VersionedState;

  [Meta, Version(2)]
  public partial record Version2 : VersionedState;
}
