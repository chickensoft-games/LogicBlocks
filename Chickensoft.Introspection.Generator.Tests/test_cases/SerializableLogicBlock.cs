namespace Chickensoft.LogicBlocks.Generator.Tests.TestCases;

using Chickensoft.Introspection;

[Introspective("serializable_logic_block")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class SerializableLogicBlock : LogicBlock<SerializableLogicBlock.State> {
  public override Transition GetInitialState() => To<State>();

  public record State : StateLogic<State>;
}
