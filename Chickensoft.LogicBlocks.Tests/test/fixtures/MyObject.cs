namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class MyObject : IDisposable {
  public MyLogicBlock Logic { get; }
  public MyLogicBlock.IBinding Binding { get; }

  public bool SawSomeOutput { get; private set; }

  public MyObject(MyLogicBlock logic) {
    Logic = logic;
    Binding = logic.Bind();

    Binding.Handle<MyLogicBlock.Output.SomeOutput>(
      (output) => SawSomeOutput = true
    );
  }

  // Method we want to test
  public MyLogicBlock.State DoSomething() =>
    Logic.Input(new MyLogicBlock.Input.SomeInput());

  public void Dispose() {
    Binding.Dispose();
    GC.SuppressFinalize(this);
  }
}
