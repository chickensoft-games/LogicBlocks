namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.Introspection;
using Shouldly;
using Xunit;

// Don't run in parallel with other LogicBlock tests.
// Global introspection state is shared.
[Collection("LogicBlock")]
public partial class PreallocationTest
{

  [LogicBlock(typeof(State))]
  public partial class RegularLogic : LogicBlock<RegularLogic.State>
  {
    public override Transition GetInitialState() => To<State>();

    public RegularLogic()
    {
      Set(new State());
    }

    public record State : StateLogic<State>;
  }

  [Meta]
  public partial class MissingLogic : LogicBlock<MissingLogic.State>
  {
    public override Transition GetInitialState() => To<State>();

    public record State : StateLogic<State>;
  }

  [LogicBlock(typeof(State)), Meta]
  public partial class MetaLogic : LogicBlock<MetaLogic.State>
  {
    public override Transition GetInitialState() => To<State>();

    public record State : StateLogic<State>;
  }

  [LogicBlock(typeof(State))]
  [Meta, Id("preallocation_serializable_logic_block")]
  public partial class SerializableLogic : LogicBlock<SerializableLogic.State>
  {
    public override Transition GetInitialState() => To<State>();

    [Meta, Id("preallocation_serializable_logic_block_state")]
    public partial record State : StateLogic<State>;

    [Meta, TestState]
    public partial record TestState : State;

    [Meta]
    public partial record OtherState : State;
  }

  [LogicBlock(typeof(State))]
  [Meta, Id("preallocation_non_id_substate_logic_block")]
  public partial class ConcreteSubstateWithoutId :
  LogicBlock<ConcreteSubstateWithoutId.State>
  {
    public override Transition GetInitialState() => To<State>();

    [Meta]
    public abstract partial record State : StateLogic<State>;

    [Meta]
    // Missing [Id]
    // Serializable logic blocks require concrete states to be identifiable.
    public partial record Substate : State;
  }

  [LogicBlock(typeof(State)), Meta, Id("not_introspective")]
  public partial class NotIntrospective : LogicBlock<NotIntrospective.State>
  {
    public override Transition GetInitialState() => To<State>();

    [Meta, Id("not_introspective_state")]
    public partial record State : StateLogic<State>;

    // Needs to be introspective and have an id since it is concrete.
    public partial record Substate : State;
    [Meta]
    public abstract partial record OtherSubstate : State;
  }

  [LogicBlock(typeof(State)), Meta, Id("not_identifiable")]
  public partial class NotIdentifiable : LogicBlock<NotIdentifiable.State>
  {
    public override Transition GetInitialState() => To<State>();

    [Meta]
    public partial record State : StateLogic<State>;
  }


  [Fact]
  public void PreallocatesCorrectStates()
  {
    var logic = new SerializableLogic();
    logic.Has<SerializableLogic.State>().ShouldBeTrue();
    logic.Has<SerializableLogic.OtherState>().ShouldBeTrue();

    // Shouldn't allocate introspective states marked with TestState.
    logic.Has<SerializableLogic.TestState>().ShouldBeFalse();
  }

  [Fact]
  public void DoesNothingIfLogicBlockIsNotIntrospective() =>
    Should.NotThrow(() => new RegularLogic());

  [Fact]
  public void ThrowsIfMissingLogicBlockAttribute() =>
    Should.Throw<LogicBlockException>(() => new MissingLogic());

  [Fact]
  public void
  ThrowsWhenConcreteStateIsMissingId() =>
    Should.Throw<LogicBlockException>(() => new ConcreteSubstateWithoutId());

  [Fact]
  public void ThrowsIfAStateIsNotIntrospective() =>
    Should.Throw<LogicBlockException>(() => new NotIntrospective());

  [Fact]
  public void ThrowsIfAStateIsNotIdentifiable() =>
    Should.Throw<LogicBlockException>(() => new NotIdentifiable());
}
