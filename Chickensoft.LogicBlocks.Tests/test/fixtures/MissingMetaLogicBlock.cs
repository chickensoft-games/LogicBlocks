namespace Chickensoft.LogicBlocks.Tests.Fixtures;

[LogicBlock(typeof(State), Diagram = false)]
public partial class MissingMetaLogicBlock : LogicBlock<MissingMetaLogicBlock.State>
{
  public override Transition GetInitialState() => To<State>();

  public partial record State : StateLogic<State>;
}
