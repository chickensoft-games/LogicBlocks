namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class LogicBlockConsumer {
  public bool SawOutput { get; private set; }
  public bool SawInput { get; private set; }
  public bool SawState { get; private set; }
  public bool SawError { get; private set; }

  public MyLogicBlock Logic { get; }
  public MyLogicBlock.IBinding Binding { get; }

  public LogicBlockConsumer(MyLogicBlock logic) {
    Logic = logic;
    Binding = logic.Bind();

    // Bind to everything possible.
    Binding
      .Watch<MyLogicBlock.Input.SomeInput>(input => SawInput = true)
      .Handle<MyLogicBlock.Output.SomeOutput>(output => SawOutput = true)
      .Catch<Exception>(e => SawError = true)
      .When<MyLogicBlock.State.SomeOtherState>()
        .Call(state => SawState = true);
  }
}
