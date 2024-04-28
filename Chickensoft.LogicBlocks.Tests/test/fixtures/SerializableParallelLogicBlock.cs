namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Introspective("serializable_parallel_logic_block")]
[LogicBlock(typeof(State), Diagram = false)]
public partial class SerializableParallelLogicBlock :
LogicBlock<SerializableParallelLogicBlock.IState> {
  public override IState GetInitialState() => new NotParallelState();

  public interface IState : IStateLogic<IState>;

  [Introspective("serializable_parallel_logic_block_state")]
  public abstract partial record State : StateLogic<IState>, IState;

  [Introspective("serializable_parallel_logic_block_state_not_parallel")]
  public partial record NotParallelState : State,
  IGet<Input.GoToParallelState> {
    public IState On(in Input.GoToParallelState input) => new ParallelState();
  }

  [Introspective("serializable_parallel_logic_block_state_parallel")]
  public partial record ParallelState : State, IState {
    [Save("state_a")]
    public SerializableLogicBlock StateA { get; set; } = new();

    [Save("state_b")]
    public SerializableLogicBlock StateB { get; set; } = new();
  }

  public static class Input {
    public readonly record struct GoToParallelState;
  }
}
