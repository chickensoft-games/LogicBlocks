namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class MyObject : IDisposable {
  public IMyLogicBlock Logic { get; }
  public MyLogicBlock.IBinding Binding { get; }

  public bool SawSomeOutput { get; private set; }

  public MyObject(IMyLogicBlock logic) {
    Logic = logic;
    Binding = logic.Bind();

    Binding.Handle(
      (in MyLogicBlock.Output.SomeOutput output) => SawSomeOutput = true
    );
  }

  // Method we want to test
  public void DoSomething() => Logic.Input(new MyLogicBlock.Input.SomeInput());

  public void Dispose() {
    Binding.Dispose();
    GC.SuppressFinalize(this);
  }
}
