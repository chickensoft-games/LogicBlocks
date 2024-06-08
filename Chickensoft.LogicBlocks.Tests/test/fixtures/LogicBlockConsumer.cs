namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;

public class LogicBlockConsumer {
  public bool SawOutput { get; private set; }
  public bool SawInput { get; private set; }
  public bool SawState { get; private set; }
  public bool SawError { get; private set; }

  public IMyLogicBlock Logic { get; }
  public MyLogicBlock.IBinding Binding { get; }

  public LogicBlockConsumer(IMyLogicBlock logic) {
    Logic = logic;
    Binding = logic.Bind();

    // Bind to everything possible.
    Binding
      .Watch((in MyLogicBlock.Input.SomeInput input) => SawInput = true)
      .When<MyLogicBlock.State.SomeOtherState>(state => SawState = true)
      .Handle((in MyLogicBlock.Output.SomeOutput output) => SawOutput = true)
      .Catch<Exception>(e => SawError = true);
  }
}
