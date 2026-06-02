namespace Chickensoft.LogicBlocks.Auto.Tests.Fixtures;

using Introspection;

[Meta]
[Id("my_logic_block")]
public partial class MyLogicBlockSaveData : ILogicBlockSaveData
{
  public required LogicBlockData Data { get; init; }
}

public class MyLogicBlock : AutoBlock
{
  // new in logic blocks 6
  public override ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    new MyLogicBlockSaveData { Data = data };
}

// --- SerializableBlock and its states ---

[Meta, Id("serializable_block_state")]
public partial record SerializableBlockState : LogicBlockState
{
  [Meta, Id("serializable_block_other_state")]
  public partial record OtherState : SerializableBlockState;

  [Meta, Id("serializable_block_skipped_test_state"), TestState]
  public partial record SkippedTestState : SerializableBlockState;
}

[Meta, Id("serializable_block")]
public partial class SerializableBlock : AutoBlock
{
  public SerializableBlock()
  {
    Preallocate<SerializableBlockState>();
  }

  public override ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    new SerializableBlockSaveData { Data = data };
}

[Meta, Id("serializable_block_save_data")]
public partial class SerializableBlockSaveData : ILogicBlockSaveData
{
  public required LogicBlockData Data { get; init; }
}

// --- NonSerializableBlock ---

[Meta]
public partial record NonSerializableBlockState : LogicBlockState;

// Non-serializable logic block — preallocation works without serialization
// registration or strict validation.
[Meta]
public partial class NonSerializableBlock : AutoBlock
{
  public NonSerializableBlock()
  {
    Preallocate<NonSerializableBlockState>();
  }

  public override ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    new MyLogicBlockSaveData { Data = data };
}

// --- MissingIdBlock ---

[Meta]
public abstract partial record MissingIdBlockState : LogicBlockState
{
  [Meta]
  // Missing [Id] — should cause preallocation to throw when used with a
  // serializable logic block.
  public partial record BadSubstate : MissingIdBlockState;
}

[Meta, Id("missing_id_block")]
public partial class MissingIdBlock : AutoBlock
{
  public MissingIdBlock()
  {
    Preallocate<MissingIdBlockState>();
  }

  public override ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    new MyLogicBlockSaveData { Data = data };
}

// --- NotIntrospectiveStateBlock ---

[Meta, Id("not_introspective_state_block_state")]
public partial record NotIntrospectiveBlockState : LogicBlockState
{
  // Not introspective at all — no [Meta].
  public record BadSubstate : NotIntrospectiveBlockState;
}

[Meta, Id("not_introspective_state_block")]
public partial class NotIntrospectiveStateBlock : AutoBlock
{
  public NotIntrospectiveStateBlock()
  {
    Preallocate<NotIntrospectiveBlockState>();
  }

  public override ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    new MyLogicBlockSaveData { Data = data };
}

// --- NotIdentifiableStateBlock ---

[Meta]
public partial record NotIdentifiableBlockState : LogicBlockState;

[Meta, Id("not_identifiable_state_block")]
public partial class NotIdentifiableStateBlock : AutoBlock
{
  public NotIdentifiableStateBlock()
  {
    Preallocate<NotIdentifiableBlockState>();
  }

  public override ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    new MyLogicBlockSaveData { Data = data };
}

// Abstract state with [Id] — for testing history deserialization with a
// non-concrete identifiable type.
[Meta, Id("abstract_state")]
public abstract partial record AbstractIdentifiableState : LogicBlockState;

// Simple [Meta]-tagged model for blackboard delegate testing (not a state).
[Meta, Id("test_model")]
public partial class TestModel
{
  public string Value { get; set; } = "default";
}

// --- LoadableBlock ---

[Meta, Id("loadable_block_state")]
public partial record LoadableBlockState : LogicBlockState;

[Meta, Id("loadable_block")]
public partial class LoadableBlock : AutoBlock
{
  public bool OnLoadCalled { get; private set; }

  public LoadableBlock()
  {
    Preallocate<LoadableBlockState>();
  }

  public override void OnLoad() => OnLoadCalled = true;

  public override ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    new MyLogicBlockSaveData { Data = data };
}

// --- ParamBlock ---

[Meta, Id("param_block_state")]
public partial record ParamBlockState : LogicBlockState;

[Meta, Id("param_block")]
public partial class ParamBlock : AutoBlock
{
  public ParamBlock()
    : base(stateTypes: [typeof(ParamBlockState)]) { }

  public override ILogicBlockSaveData GetSaveData(LogicBlockData data) =>
    new MyLogicBlockSaveData { Data = data };
}
