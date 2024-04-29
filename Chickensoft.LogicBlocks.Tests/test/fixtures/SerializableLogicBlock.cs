namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

[Introspective("serializable_logic_block")]
[LogicBlock(typeof(State), Diagram = false)]
public partial class SerializableLogicBlock :
LogicBlock<SerializableLogicBlock.State> {
  public override Transition GetInitialState() => To<State>();

  [Introspective("serializable_logic_block_state")]
  public partial record State : StateLogic<State> {
  }
}
