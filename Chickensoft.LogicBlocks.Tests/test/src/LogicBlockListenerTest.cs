namespace Chickensoft.LogicBlocks.Tests;

using System;
using Chickensoft.Introspection;
using Moq;
using Shouldly;
using Xunit;

public partial class LogicBlockListenerTest
{
  public interface ITestLogic : ILogicBlock<TestLogic.State>;

  [Meta, Id("logic_block_listener_test_logic")]
  [LogicBlock(typeof(State))]
  public partial class TestLogic : LogicBlock<TestLogic.State>
  {
    public override Transition GetInitialState() => To<State>();

    public sealed record State : StateLogic<State> { }
  }

  public readonly record struct ValueType;

  [Fact]
  public void ImplementsListenerMethodsThatDoNothing()
  {
    var logic = new Mock<ITestLogic>();
    var listener = new LogicBlockListener<TestLogic.State>(logic.Object) as ILogicBlockBinding<TestLogic.State>;

    Should.NotThrow(() => listener.MonitorInput(new ValueType()));
    Should.NotThrow(() => listener.MonitorState(new TestLogic.State()));
    Should.NotThrow(() => listener.MonitorOutput(new ValueType()));
    Should.NotThrow(
      () => listener.MonitorException(new InvalidOperationException())
    );
  }
}
