namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

[Meta, Id("serializable_logic_block_with_on_enter")]
[LogicBlock(typeof(State), Diagram = false)]
public partial class SerializableLogicBlockWithOnEnter :
LogicBlock<SerializableLogicBlockWithOnEnter.State>
{
  public override Transition GetInitialState() => To<StateA>();

  public SerializableLogicBlockWithOnEnter()
  {
    Set(new Data());
  }

  public record Data
  {
    public bool AutomaticallyLeaveAOnEnter { get; set; }
  }

  public static class Input
  {
    public readonly record struct GoToA;
    public readonly record struct GoToB;
  }

  public static class Output
  {
    public readonly record struct StateEntered;
    public readonly record struct StateAEntered;
    public readonly record struct StateBEntered;
  }

  [Meta]
  public abstract partial record State : StateLogic<State>,
  IGet<Input.GoToA>, IGet<Input.GoToB>
  {
    public State()
    {
      this.OnEnter(() => Output(new Output.StateEntered()));
    }

    public Transition On(in Input.GoToA input) => To<StateA>();
    public Transition On(in Input.GoToB input) => To<StateB>();
  }

  [Meta, Id("serializable_logic_block_with_on_enter_state_a")]
  public partial record StateA : State
  {
    public StateA()
    {
      this.OnEnter(() =>
      {
        Output(new Output.StateAEntered());
        if (Get<Data>().AutomaticallyLeaveAOnEnter)
        {
          Input(new Input.GoToB());
        }
      });
    }
  }

  [Meta, Id("serializable_logic_block_with_on_enter_state_b")]
  public partial record StateB : State
  {
    public StateB()
    {
      this.OnEnter(() => Output(new Output.StateBEntered()));
    }
  }
}
