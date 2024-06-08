namespace Chickensoft.LogicBlocks.Tutorial;

using Chickensoft.Introspection;

public interface ISerializableLogicBlock : ILogicBlock<SerializableLogicBlock.State>;

[Meta, LogicBlock(typeof(State), Diagram = true), Id("serializable_logic")]
public partial class SerializableLogicBlock :
LogicBlock<SerializableLogicBlock.State>, ISerializableLogicBlock {
  public override Transition GetInitialState() => To<State.PoweredOff>();

  [Meta]
  public abstract partial record State : StateLogic<State> {
    [Meta, Id("serializable_logic_state_off")]
    public partial record PoweredOff : State;

    [Meta, Id("serializable_logic_state_on")]
    public partial record PoweredOn : State;

    [Meta, Id("serializable_logic_versioned_state")]
    public abstract partial record VersionedState : State;

    [Meta, Version(1)]
    public partial record Version1 : VersionedState;

    [Meta, Version(2)]
    public partial record Version2 : VersionedState;
  }
}
