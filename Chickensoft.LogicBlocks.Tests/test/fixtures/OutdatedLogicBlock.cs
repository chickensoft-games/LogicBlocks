namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Collections;
using Chickensoft.Introspection;
using Chickensoft.Serialization;


[LogicBlock(typeof(State), Diagram = false), Meta, Id("outdated_logic_block")]
public partial class OutdatedLogicBlock : LogicBlock<OutdatedLogicBlock.State> {
  public override Transition GetInitialState() => To<V1>();

  [Meta, Id("outdated_logic_block_state")]
  public abstract partial record State : StateLogic<State>;

  [Meta, Id("outdated_logic_block_state_v1")]
  public partial record V1 : State, IOutdated {
    public object Upgrade(IReadOnlyBlackboard blackboard) => new V2();
  }

  [Meta, Id("outdated_logic_block_state_v2")]
  public partial record V2 : State, IOutdated {
    public object Upgrade(IReadOnlyBlackboard blackboard) => new V3();
  }

  [Meta, Id("outdated_logic_block_state_v3")]
  public partial record V3 : State;
}
