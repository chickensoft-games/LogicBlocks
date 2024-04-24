namespace Chickensoft.LogicBlocks.Tests;

using System;
using Moq;
using Shouldly;
using Xunit;

public class LogicBlockListenerTest {
  public interface ITestLogic : ILogicBlock<TestLogic.IState> { }

  [LogicBlock(typeof(State))]
  public class TestLogic : LogicBlock<TestLogic.IState> {
    public override IState GetInitialState() => new State();

    public interface IState : IStateLogic<IState> { }
    public sealed record State : StateLogic<IState>, IState { }
  }

  public readonly record struct ValueType;

  [Fact]
  public void ImplementsListenerMethodsThatDoNothing() {
    var logic = new Mock<ITestLogic>();
    var listener = new LogicBlockListener<TestLogic.IState>(logic.Object) as ILogicBlockBinding<TestLogic.IState>;

    Should.NotThrow(() => listener.MonitorInput(new ValueType()));
    Should.NotThrow(() => listener.MonitorState(new TestLogic.State()));
    Should.NotThrow(() => listener.MonitorOutput(new ValueType()));
    Should.NotThrow(
      () => listener.MonitorException(new InvalidOperationException())
    );
  }
}
