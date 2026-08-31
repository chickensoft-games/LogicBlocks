namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class SharedBaseStateLogic : LogicBlock
{
  public SharedBaseStateLogic()
  {
    Set(new SharedState.Lit());
    Set(new SharedState.Dark());
  }

  public void StartLogicBlock() => Start<SharedState.Dark>();

  public static class Input
  {
    public readonly record struct Toggle;
  }

  public static class Output
  {
    public readonly record struct Toggled;
  }

  [StateDiagram]
  public abstract partial record SharedState : SharedBaseState;
}
