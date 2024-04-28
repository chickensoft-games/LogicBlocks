namespace Chickensoft.LogicBlocks.Generator.Tests.TestCases;

using Chickensoft.Introspection;

[Introspective("serializable_logic_block")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class SerializableLogicBlock : LogicBlock<SerializableLogicBlock.IState> {
  public override IState GetInitialState() => new State();

  public interface IState : IStateLogic<IState>;

  public record State : StateLogic<IState>, IState { }
}
