namespace Chickensoft.LogicBlocks.Tests;

using System.Collections.Generic;
using Chickensoft.LogicBlocks;
using Shouldly;
using Xunit;

public class InputQueueTests {
  public readonly record struct InputA;
  public readonly record struct InputB;

  public class TestInputHandler : IInputHandler {
    public List<object> Inputs { get; } = new();
    void IInputHandler.HandleInput<TInputType>(in TInputType input) =>
      Inputs.Add(input);
  }

  [Fact]
  public void Initializes() {
    var handler = new TestInputHandler();
    var queue = new InputQueue(handler);

    queue.Handler.ShouldBe(handler);
  }

  [Fact]
  public void EnqueueAndHandleInputs() {
    var handler = new TestInputHandler();
    var queue = new InputQueue(handler);

    var inputA = new InputA();
    var inputA2 = new InputA();
    var inputB = new InputB();

    queue.Enqueue(inputA);
    queue.Enqueue(inputA2);
    queue.Enqueue(inputB);

    queue.HasInputs.ShouldBeTrue();

    queue.HandleInput();
    queue.HandleInput();
    queue.HandleInput();

    queue.HasInputs.ShouldBeFalse();
    handler.Inputs.ShouldBe(new object[] { inputA, inputA2, inputB });
  }

  [Fact]
  public void ClearQueue() {
    var handler = new TestInputHandler();
    var queue = new InputQueue(handler);

    queue.Enqueue(new InputA());
    queue.Enqueue(new InputB());

    queue.Clear();

    queue.HasInputs.ShouldBeFalse();
  }
}
