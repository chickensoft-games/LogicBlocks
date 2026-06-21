namespace Chickensoft.LogicBlocks.Auto.Tests;

using Collections;
using Fixtures;
using Shouldly;

public class AutoBlockTest
{
  [Fact]
  public void SaveReturnsSaveData()
  {
    using var logic = new SerializableBlock();
    logic.Start<SerializableBlockState>();

    var saveData = logic.GetSaveData();

    saveData.ShouldNotBeNull();
    saveData.Data.StateType.ShouldBe(typeof(SerializableBlockState));
  }

  [Fact]
  public void LoadedCallsOnLoad()
  {
    using var logic = new LoadableBlock();

    var bb = new Blackboard();
    bb.Set(new LoadableBlockState());
    var data = new LogicBlockData(
      typeof(LoadableBlockState), bb, new History()
    );

    logic.Start(data);

    logic.OnLoadCalled.ShouldBeTrue();
    logic.State.ShouldBeOfType<LoadableBlockState>();
  }

  [Fact]
  public void ConstructorWithStateTypesPreallocates()
  {
    using var logic = new ParamBlock();

    logic.Has<ParamBlockState>().ShouldBeTrue();
  }

  [Fact]
  public void BlackboardDelegatesForwardToSerializableBlackboard()
  {
    using var logic = new MyLogicBlock();

    logic.SavedTypes.ShouldNotBeNull();
    logic.TypesToSave.ShouldNotBeNull();
    logic.Save(() => new TestModel { Value = "test" });
    logic.SavedTypes.ShouldContain(typeof(TestModel));

    logic.SaveObject(
      typeof(SerializableBlockState.OtherState),
      () => new SerializableBlockState.OtherState(),
      null
    );
    logic.SavedTypes.ShouldContain(typeof(SerializableBlockState.OtherState));
  }

  [Fact]
  public void LoadedCallsBaseOnLoadWhenNotOverridden()
  {
    using var logic = new SerializableBlock();

    var bb = new Blackboard();
    bb.Set(new SerializableBlockState());
    var data = new LogicBlockData(
      typeof(SerializableBlockState), bb, new History()
    );

    // SerializableBlock does not override OnLoad()
    logic.Start(data);

    logic.IsStarted.ShouldBeTrue();
    logic.State.ShouldBeOfType<SerializableBlockState>();
  }
}
