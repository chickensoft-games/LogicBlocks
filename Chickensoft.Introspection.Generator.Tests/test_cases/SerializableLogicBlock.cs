namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

[Meta("serializable_logic_block")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class SerializableLogicBlock : LogicBlock<SerializableLogicBlock.State> {
  public override Transition GetInitialState() => To<State>();

  public record State : StateLogic<State>;
}
