namespace Chickensoft.LogicBlocks.ScratchPad;

public partial class OverriddenHandlers : LogicBlock
{
  public OverriddenHandlers()
  {
    Set(new BaseState.Idle());
  }

  public void StartLogicBlock() => Start<BaseState.Idle>();
}
