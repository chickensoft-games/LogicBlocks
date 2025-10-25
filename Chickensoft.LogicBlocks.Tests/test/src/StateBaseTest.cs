namespace Chickensoft.LogicBlocks.Tests;

using System;
using Moq;
using Shouldly;
using Xunit;

public class StateBaseTest
{
  private interface ITestLogic : ILogicBlock<TestLogic.State>;

  [LogicBlock(typeof(State))]
  private sealed class TestLogic : LogicBlock<TestLogic.State>
  {
    public required Func<Transition> InitialState { get; init; }

    public override Transition GetInitialState() => InitialState();

    public Action<Exception>? ErrorHandler { get; init; }

    public sealed record State : StateLogic<State>
    {
      public State(Action callback)
      {
        OnAttach(callback);
      }
    }
    protected override void HandleError(Exception e) => ErrorHandler?.Invoke(e);
  }

  [Fact]
  public void AttachmentCallbackErrorsRethrow()
  {
    var adapter = new TestLogic.ContextAdapter();
    var context = new Mock<IContext>();

    adapter.Adapt(context.Object);

    var state =
      new TestLogic.State(() => throw new InvalidOperationException());

    Should.Throw<InvalidOperationException>(() => state.Attach(adapter));
  }

  [Fact]
  public void AttachmentCallbackErrorsGetHandled()
  {
    var called = false;
    var logic = new TestLogic()
    {
      InitialState =
        () => new(
          new TestLogic.State(() => throw new InvalidOperationException())
        ),
      ErrorHandler = e => called = true
    };

    Should.NotThrow(logic.Start);

    called.ShouldBeTrue();
  }
}
