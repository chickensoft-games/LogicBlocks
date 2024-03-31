namespace Chickensoft.LogicBlocks.Tests;

using System;
using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class FakeBindingTest {
  [Fact]
  public void SimulatesAnInput() {
    var logic = new Mock<MyLogicBlock>();
    var binding = MyLogicBlock.CreateFakeBinding();
    logic.Setup(logic => logic.Bind()).Returns(binding);

    var consumer = new LogicBlockConsumer(logic.Object);

    binding.Input(new MyLogicBlock.Input.SomeInput());

    consumer.SawInput.ShouldBeTrue();
  }

  [Fact]
  public void SimulatesAnOutput() {
    var logic = new Mock<MyLogicBlock>();
    var binding = MyLogicBlock.CreateFakeBinding();
    logic.Setup(logic => logic.Bind()).Returns(binding);

    var consumer = new LogicBlockConsumer(logic.Object);

    binding.Output(new MyLogicBlock.Output.SomeOutput());

    consumer.SawOutput.ShouldBeTrue();
  }

  [Fact]
  public void SimulatesAnError() {
    var logic = new Mock<MyLogicBlock>();
    var binding = MyLogicBlock.CreateFakeBinding();
    logic.Setup(logic => logic.Bind()).Returns(binding);

    var consumer = new LogicBlockConsumer(logic.Object);

    binding.AddError(new InvalidOperationException());

    consumer.SawError.ShouldBeTrue();
  }

  [Fact]
  public void SimulatesAState() {
    var logic = new Mock<MyLogicBlock>();
    var binding = MyLogicBlock.CreateFakeBinding();
    logic.Setup(logic => logic.Bind()).Returns(binding);

    var consumer = new LogicBlockConsumer(logic.Object);

    binding.SetState(new MyLogicBlock.State.SomeOtherState());

    consumer.SawState.ShouldBeTrue();
  }
}
