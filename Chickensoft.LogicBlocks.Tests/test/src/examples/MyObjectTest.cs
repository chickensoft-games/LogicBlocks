namespace Chickensoft.LogicBlocks.Tests.Examples;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

public class MyObjectTest {
  [Fact]
  public void DoSomethingDoesSomething() {
    // Our unit test follows the AAA pattern: Arrange, Act, Assert.
    // Or Setup, Execute, and Verify, if you prefer.

    // Setup — make a fake binding and return that from our mock logic block
    using var binding = MyLogicBlock.CreateFakeBinding();

    var logic = new Mock<IMyLogicBlock>();
    logic.Setup(logic => logic.Bind()).Returns(binding);
    logic.Setup(logic => logic.Input(It.IsAny<MyLogicBlock.Input.SomeInput>()));

    using var myObject = new MyObject(logic.Object);

    // Execute — run the method we're testing
    myObject.DoSomething();

    // Verify — check that the mock object's stubbed methods were called
    logic.VerifyAll();
  }

  [Fact]
  public void HandlesSomeOutput() {
    // Setup — make a fake binding and return that from our mock logic block
    using var binding = MyLogicBlock.CreateFakeBinding();

    var logic = new Mock<IMyLogicBlock>();
    logic.Setup(logic => logic.Bind()).Returns(binding);

    using var myObject = new MyObject(logic.Object);

    // Execute — trigger an output from the fake binding!
    binding.Output(new MyLogicBlock.Output.SomeOutput());

    // Verify — verify object's callback was invoked by checking side effects
    myObject.SawSomeOutput.ShouldBeTrue();
  }
}
