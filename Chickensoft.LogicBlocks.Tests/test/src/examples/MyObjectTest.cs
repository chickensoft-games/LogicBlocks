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

    var myObject = new MyObject(logic.Object);

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
}
