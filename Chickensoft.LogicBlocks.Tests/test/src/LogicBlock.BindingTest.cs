namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Chickensoft.LogicBlocks.Tests.TestUtils;
using Shouldly;
using Xunit;

public class BlocGlueTests {
  public static bool WasFinalized { get; set; }

  [Fact]
  public void DoesNotUpdateIfStateIsSameState() { }

  [Fact]
  public void DoesNotUpdateIfSelectedDataIsSameObject() {
    var block = new FakeLogicBlock();
    using var binding = block.Bind();

    var count = 0;
    var value1Count = 0;
    binding.When<FakeLogicBlock.State.StateB>()
      .Call(state => count++)
      .Use(
        data: (state) => state.Value1,
        to: (value1) => value1Count++
      );

    var a = "a";
    var b = "b";
    block.Input(new FakeLogicBlock.Input.InputTwo(a, b));
    block.Input(new FakeLogicBlock.Input.InputTwo(a, "c"));

    count.ShouldBe(1);
  }

  [Fact]
  public void UpdatesCorrectly() {
    var block = new FakeLogicBlock();
    using var binding = block.Bind();

    var callA1 = 0;
    var callA2 = 0;

    var a1 = 3;
    var a2 = 4;

    binding.When<FakeLogicBlock.State.StateA>()
      .Use(
        data: (state) => state.Value1,
        to: (value1) => { callA1++; value1.ShouldBe(a1); })
      .Use(
        data: (state) => state.Value2,
        to: (value2) => { callA2++; value2.ShouldBe(a2); }
      );

    callA1.ShouldBe(0);
    callA2.ShouldBe(0);

    block.Input(new FakeLogicBlock.Input.InputOne(a1, a2));

    callA1.ShouldBe(1);
    callA2.ShouldBe(1);

    // Make sure the same values don't trigger the actions again

    a1 = 5;
    block.Input(new FakeLogicBlock.Input.InputOne(a1, a2));

    callA1.ShouldBe(2);
    callA2.ShouldBe(1);

    // Make sure unrelated events don't trigger the actions

    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callA1.ShouldBe(2);
    callA2.ShouldBe(1);

    // Make sure that previous unrelated states cause actions for new state
    // to be called

    block.Input(new FakeLogicBlock.Input.InputOne(a1, a2));

    callA1.ShouldBe(3);
    callA2.ShouldBe(2);
  }

  [Fact]
  public void HandlesEffects() {
    var block = new FakeLogicBlock();
    using var binding = block.Bind();

    var callEffect1 = 0;
    var callEffect2 = 0;

    binding.Handle<FakeLogicBlock.Output.OutputOne>(
      (effect) => { callEffect1++; effect.Value.ShouldBe(1); }
    ).Handle<FakeLogicBlock.Output.OutputTwo>(
      (effect) => { callEffect2++; effect.Value.ShouldBe("2"); }
    );

    // Effects should get handled each time, regardless of if they are
    // identical to the previous one.

    block.Input(new FakeLogicBlock.Input.InputOne(1, 2));
    block.Input(new FakeLogicBlock.Input.InputOne(1, 2));

    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));
    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callEffect1.ShouldBe(2);
    callEffect2.ShouldBe(2);
  }

  [Fact]
  public void CallsSubstateTransitionsOnlyOnce() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.Context(block);

    using var binding = block.Bind();

    var callStateA = 0;
    var callStateB = 0;

    binding.When<FakeLogicBlock.State.StateA>()
      .Call((state) => callStateA++);

    binding.When<FakeLogicBlock.State.StateB>()
      .Call((state) => callStateB++);

    callStateA.ShouldBe(0);
    callStateB.ShouldBe(0);
    block.Value.ShouldBe(block.GetInitialState(context));

    // State is StateA initially, so switch to State B
    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callStateA.ShouldBe(0);
    callStateB.ShouldBe(1);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();

    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callStateA.ShouldBe(0);
    callStateB.ShouldBe(1);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();

    block.Input(new FakeLogicBlock.Input.InputTwo("c", "d"));

    callStateA.ShouldBe(0);
    callStateB.ShouldBe(1);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();

    block.Input(new FakeLogicBlock.Input.InputOne(1, 2));

    callStateA.ShouldBe(1);
    callStateB.ShouldBe(1);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateA>();

    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callStateA.ShouldBe(1);
    callStateB.ShouldBe(2);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();
  }

  [Fact]
  public void CleansUpSubscriptions() {
    var callStateUpdate = 0;
    var callSideEffectHandler = 0;

    var block = new FakeLogicBlock();
    var binding = block.Bind();

    binding.When<FakeLogicBlock.State.StateA>()
      .Use(
        data: (state) => state.Value1,
        to: (value1) => callStateUpdate++
      );

    binding.Handle<FakeLogicBlock.Output.OutputOne>(
      (effect) => callSideEffectHandler++
    );

    block.Input(new FakeLogicBlock.Input.InputOne(4, 5));

    callStateUpdate.ShouldBe(1);
    callSideEffectHandler.ShouldBe(1);

    binding.Dispose();

    block.Input(new FakeLogicBlock.Input.InputOne(5, 6));

    callStateUpdate.ShouldBe(1);
    callSideEffectHandler.ShouldBe(1);
  }

  [Fact]
  public void Finalizes() {
    // Weak reference has to be created and cleared from a static function
    // or else the GC won't ever collect it :P
    var weakRef = CreateWeakFakeLogicBlockBindingReference();
    Utils.ClearWeakReference(weakRef);
  }

  public static WeakReference CreateWeakFakeLogicBlockBindingReference() =>
    new(new FakeLogicBlock().Bind());
}
