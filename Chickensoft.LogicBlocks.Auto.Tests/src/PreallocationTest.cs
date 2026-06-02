namespace Chickensoft.LogicBlocks.Auto.Tests;

using Fixtures;
using Shouldly;

public class PreallocationTest
{
  [Fact]
  public void PreallocatesConcreteStatesOntoBlackboard()
  {
    var logic = new SerializableBlock();

    logic.Has<SerializableBlockState>().ShouldBeTrue();
    logic.Has<SerializableBlockState.OtherState>().ShouldBeTrue();
  }

  [Fact]
  public void SkipsTestStates()
  {
    var logic = new SerializableBlock();

    logic.Has<SerializableBlockState.SkippedTestState>().ShouldBeFalse();
  }

  [Fact]
  public void CachesReferenceStates()
  {
    AutoBlock.ReferenceStates.Clear();

    var _ = new SerializableBlock();

    AutoBlock.ReferenceStates
      .ContainsKey(typeof(SerializableBlockState)).ShouldBeTrue();
    AutoBlock.ReferenceStates
      .ContainsKey(typeof(SerializableBlockState.OtherState)).ShouldBeTrue();
  }

  [Fact]
  public void WorksForNonSerializableBlocks()
  {
    var logic = new NonSerializableBlock();

    logic.Has<NonSerializableBlockState>().ShouldBeTrue();
  }

  [Fact]
  public void ThrowsWhenConcreteStateMissingIdOnSerializableBlock()
  {
    Should.Throw<LogicBlockException>(() => new MissingIdBlock());
  }

  [Fact]
  public void ThrowsWhenStateNotIntrospectiveOnSerializableBlock()
  {
    Should.Throw<LogicBlockException>(
      () => new NotIntrospectiveStateBlock()
    );
  }

  [Fact]
  public void ThrowsWhenBaseStateIsNotIdentifiable()
  {
    Should.Throw<LogicBlockException>(
      () => new NotIdentifiableStateBlock()
    );
  }

}
