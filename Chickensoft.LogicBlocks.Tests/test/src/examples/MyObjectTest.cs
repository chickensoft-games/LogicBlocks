namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class MyObjectTest {
  [Fact]
  public void DoSomethingDoesSomething() {
    // Our unit test follows the AAA pattern: Arrange, Act, Assert.
    // Or Setup, Execute, and Verify, if you prefer. Etc.

    // Setup
    var logic = new Mock<MyLogicBlock>();
    var context = new Mock<MyLogicBlock.IContext>();

    var binding = new Mock<MyLogicBlock.IBinding>();
    logic.Setup(logic => logic.Bind()).Returns(binding.Object);

    using var myObject = new MyObject(logic.Object);

    // Create a state that we expect to be returned.
    var expectedState = new MyLogicBlock.State.SomeState(context.Object);

    // Setup the mock of the logic block to return our expected state whenever
    // it receives the input SomeInput.
    logic.Setup(logic => logic.Input(It.IsAny<MyLogicBlock.Input.SomeInput>()))
      .Returns(expectedState);

    // Execute the method we want to test.
    var result = myObject.DoSomething();

    // Verify that method returned the correct value.
    result.ShouldBe(expectedState);

    // Verify that the method invoked our logic block as expected.
    logic.VerifyAll();
  }

  [Fact]
  public void ListensForSomeOutput() {
    var logic = new Mock<MyLogicBlock>();
    var context = new Mock<MyLogicBlock.IContext>();
    var binding = new Mock<MyLogicBlock.IBinding>();

    // Return mocked binding so we can test binding handlers manually.
    logic.Setup(logic => logic.Bind()).Returns(binding.Object);

    var output = new MyLogicBlock.Output.SomeOutput();
    Action<MyLogicBlock.Output.SomeOutput> handler = (output) => { };

    // Capture the binding handler so we can test it.
    binding
      .Setup(binding => binding.Handle(
        It.IsAny<Action<MyLogicBlock.Output.SomeOutput>>()
      ))
      .Returns(binding.Object)
      .Callback<Action<MyLogicBlock.Output.SomeOutput>>(
        action => handler = action
      );

    using var myObject = new MyObject(logic.Object);

    // Test the binding handler.
    handler(output);

    binding.VerifyAll();
    myObject.SawSomeOutput.ShouldBeTrue();
  }
}
