namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.Introspection;
using Shouldly;
using Xunit;

// Don't run in parallel with other LogicBlock tests.
// Global introspection state is shared.
[Collection("LogicBlock")]
public partial class PreallocationTest {

  [LogicBlock(typeof(State))]
  public partial class RegularLogic : LogicBlock<RegularLogic.State> {
    public override Transition GetInitialState() => To<State>();

    public RegularLogic() {
      Set(new State());
    }

    public record State : StateLogic<State>;
  }

  [LogicBlock(typeof(State)), Meta]
  public partial class MissingLogic : LogicBlock<MissingLogic.State> {
    public override Transition GetInitialState() => To<State>();

    public record State : StateLogic<State>;
  }

  [LogicBlock(typeof(State)), Meta]
  public partial class MetaLogic : LogicBlock<MetaLogic.State> {
    public override Transition GetInitialState() => To<State>();

    public record State : StateLogic<State>;
  }

  [LogicBlock(typeof(State))]
  [Meta, Id("preallocation_serializable_logic_block")]
  public partial class SerializableLogic : LogicBlock<SerializableLogic.State> {
    public override Transition GetInitialState() => To<State>();

    [Meta, Id("preallocation_serializable_logic_block_state")]
    public partial record State : StateLogic<State>;
  }

  [LogicBlock(typeof(State))]
  [Meta, Id("preallocation_non_id_substate_logic_block")]
  public partial class NonIdSubstate : LogicBlock<NonIdSubstate.State> {
    public override Transition GetInitialState() => To<State>();

    [Meta, Id("preallocation_non_id_substate_logic_block_state")]
    public abstract partial record State : StateLogic<State>;

    [Meta] // Missing [Id]
    public partial record Substate : State;
  }

  [Fact]
  public void DoesNothingIfLogicBlockIsNotIntrospective() =>
    Should.NotThrow(() => new RegularLogic());

  [Fact]
  public void DoesNothingIfMissingLogicBlockAttribute() =>
    Should.NotThrow(() => new MissingLogic());

  [Fact]
  public void
  ThrowsWhenSerializableLogicBlockBaseStateOrSubstatesAreNotSerializable() =>
    Should.Throw<LogicBlockException>(() => new NonIdSubstate());
}
