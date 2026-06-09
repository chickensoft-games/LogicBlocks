namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Collections;

public record TestLogicBlockState : LogicBlockState,
  IGet<TestLogicBlockState.Input.TestInput>
{
  public record EveryInputState : TestLogicBlockState, IGetEveryInput
  {
    public List<object> Inputs { get; } = [];

    public Type On<TInputType>(in TInputType input)
      where TInputType : struct
    {
      Inputs.Add(input);
      return ToSelf();
    }
  }

  public Action OnEnterAction { get; set; } = () => { };
  public Action OnExitAction { get; set; } = () => { };
  public Action<object> OnInputAction { get; set; } = _ => { };

  public TestLogicBlockState()
  {
    this.OnEnter(() => OnEnterAction());
    this.OnExit(() => OnExitAction());
  }

  public static class Input
  {
    public readonly record struct TestInput;
  }

  public static class Output
  {
    public readonly record struct TestOutput;
  }

  public Type On(in Input.TestInput input)
  {
    OnInputAction(input);
    return ToSelf();
  }

  public record SubState : TestLogicBlockState;

  public record OutputtingState : TestLogicBlockState,
    IGet<Input.TestInput>
  {
    public new Type On(in Input.TestInput input)
    {
      Output(new Output.TestOutput());
      return ToSelf();
    }
  }
}

public class TestLogicBlockWithInitialState : LogicBlock
{
  public TestLogicBlockWithInitialState()
  {
    Set(new TestLogicBlockState());
  }
}

public class TestLogicBlock : LogicBlock
{
  public Action OnStartAction { get; set; } = () => { };
  public Action OnStopAction { get; set; } = () => { };


  public TestLogicBlock(
    Blackboard? blackboard = null,
    int? maxHistoryCapacity = MAX_HISTORY_DEFAULT
  ) : base(
    blackboard,
    maxHistoryCapacity
  )
  {
  }

  public override void OnStart() => OnStartAction();
  public override void OnStop() => OnStopAction();
}
