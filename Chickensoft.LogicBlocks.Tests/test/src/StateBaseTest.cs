namespace Chickensoft.LogicBlocks.Tests;

using Moq;
using Shouldly;
using Xunit;

public class StateBaseTest {
  private interface ITestLogic : ILogicBlock<TestLogic.IState> { }

  private sealed class TestLogic : LogicBlock<TestLogic.IState> {
    public required State InitialState { get; init; }

    public override IState GetInitialState() => InitialState;

    public Action<Exception>? ErrorHandler { get; init; }

    public interface IState : IStateLogic<IState> { }
    public sealed record State : StateLogic<IState>, IState {
      public State(Action callback) {
        OnAttach(callback);
      }
    }
    protected override void HandleError(Exception e) => ErrorHandler?.Invoke(e);
  }

  [Fact]
  public void AttachmentCallbackErrorsRethrow() {
    var adapter = new TestLogic.ContextAdapter();
    var context = new Mock<IContext>();

    adapter.Adapt(context.Object);

    var state =
      new TestLogic.State(() => throw new InvalidOperationException());

    Should.Throw<InvalidOperationException>(() => state.Attach(adapter));
  }


  [Fact]
  public void AttachmentCallbackErrorsGetHandled() {
    var called = false;
    var logic = new TestLogic() {
      InitialState =
        new TestLogic.State(() => throw new InvalidOperationException()),
      ErrorHandler = e => called = true
    };

    Should.NotThrow(() => logic.Start());

    called.ShouldBeTrue();
  }

  [Fact]
  public void AttachmentCallbackLogicBlockErrorsArePropagatedAlways() {
    var called = false;
    var logic = new TestLogic() {
      InitialState = new TestLogic.State(
        () => throw new LogicBlockException("message")
      ),
      ErrorHandler = e => called = true
    };

    // LogicBlockExceptions always get re-thrown, even if we have a handler.
    // Should throw AND call our error handler.
    Should.Throw<LogicBlockException>(() => logic.Start());
    called.ShouldBeTrue();
  }
}
