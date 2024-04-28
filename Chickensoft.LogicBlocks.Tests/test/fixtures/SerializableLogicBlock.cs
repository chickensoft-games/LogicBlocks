namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

[Introspective("serializable_logic_block")]
[LogicBlock(typeof(State), Diagram = false)]
public partial class SerializableLogicBlock :
LogicBlock<SerializableLogicBlock.IState> {
  public override IState GetInitialState() => new State();

  public interface IState : IStateLogic<IState>;

  [Introspective("serializable_logic_block_state")]
  public partial record State : StateLogic<IState>, IState {
  }
}
