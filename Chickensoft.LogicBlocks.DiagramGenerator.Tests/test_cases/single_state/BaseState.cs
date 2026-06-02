namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class SingleState
{
  public static class Input
  {
    public readonly record struct MyInput;
  }

  [StateDiagram]
  public record BaseState : LogicBlockState, IGet<Input.MyInput>
  {
    public BaseState()
    {
      this.OnEnter(() => Output(new Output.MyOutput()));
      this.OnExit(() => Output(new Output.MyOutput()));
    }

    public Type On(in Input.MyInput input)
    {
      Output(new Output.MyOutput());
      return ToSelf();
    }
  }

  public static class Output
  {
    public readonly record struct MyOutput;
  }
}
