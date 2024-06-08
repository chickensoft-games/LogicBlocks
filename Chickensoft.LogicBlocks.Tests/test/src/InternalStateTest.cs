namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class InternalSharedStateTest {
  [Fact]
  public void InteractsWithUnderlyingContext() {
    var attachCalled = false;
    var detachCalled = false;

    var state = new InternalsLogic.State() {
      OnAttachAction = () => attachCalled = true,
      OnDetachAction = () => detachCalled = true
    };

    var context = state.CreateFakeContext();
    // Subsequent creations should return the same fake context.
    state.CreateFakeContext().ShouldBe(context);

    context.Set("string");

    state.Attach(context);

    state.PublicGet<string>().ShouldBe("string");
    attachCalled.ShouldBeTrue();

    state.Detach();
    state.Detach(); // Detaching when already detached should do nothing.

    detachCalled.ShouldBeTrue();
  }

  [Fact]
  public void EqualsAnythingElse() {
    var state = new InternalState();
    state.Equals(new object()).ShouldBeTrue();
  }
}
