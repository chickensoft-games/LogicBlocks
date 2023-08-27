namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class MyObject {
  public MyLogicBlock Logic { get; }

  public MyObject(MyLogicBlock logic) {
    Logic = logic;
  }

  // Method we want to test
  public MyLogicBlock.State DoSomething() =>
    Logic.Input(new MyLogicBlock.Input.SomeInput());
}
