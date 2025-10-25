namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Meta, Id("serializable_parallel_logic_block")]
[LogicBlock(typeof(State), Diagram = false)]
public partial class SerializableParallelLogicBlock :
LogicBlock<SerializableParallelLogicBlock.State>
{
  public override Transition GetInitialState() => To<NotParallelState>();

  [Meta]
  public abstract partial record State : StateLogic<State>;

  [Meta, Id("serializable_parallel_logic_block_state_not_parallel")]
  public partial record NotParallelState : State,
  IGet<Input.GoToParallelState>
  {
    public Transition On(in Input.GoToParallelState input) => To<ParallelState>();
  }

  [Meta, Id("serializable_parallel_logic_block_state_parallel")]
  public partial record ParallelState : State
  {
    [Save("state_a")]
    public SerializableLogicBlock StateA { get; set; } = new();

    [Save("state_b")]
    public SerializableLogicBlock StateB { get; set; } = new();
  }

  public static class Input
  {
    public readonly record struct GoToParallelState;
  }
}
