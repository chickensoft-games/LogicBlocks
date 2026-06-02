namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class AsyncLogic : LogicBlock
{
  public AsyncLogic()
  {
    Set(new AsyncState.Idle());
    Set(new AsyncState.Loading());
    Set(new AsyncState.Loaded());
    Set(new AsyncState.Failed());
    Set(new AsyncState.Canceled());
  }
}

[StateDiagram]
public abstract record AsyncState : LogicBlockState
{
  public static class Input
  {
    public readonly record struct Fetch(Task<string> DataTask);
    public readonly record struct Succeed(string Data);
    public readonly record struct Fail(Exception Error);
    public readonly record struct Cancel();
  }

  public static class Output
  {
    public readonly record struct FetchStarted();
    public readonly record struct FetchCompleted(string Data);
    public readonly record struct FetchFailed(Exception Error);
    public readonly record struct FetchCanceled();
  }

  public sealed record Idle : AsyncState, IGet<Input.Fetch>
  {
    public Type On(in Input.Fetch input)
    {
      Output(new Output.FetchStarted());

      Async(input.DataTask)
        .Input(data => new Input.Succeed(data))
        .ErrorInput(ex => new Input.Fail(ex))
        .CanceledInput(() => new Input.Cancel());

      return To<Loading>();
    }
  }

  public sealed record Loading : AsyncState,
    IGet<Input.Succeed>,
    IGet<Input.Fail>,
    IGet<Input.Cancel>
  {
    public Type On(in Input.Succeed input)
    {
      Output(new Output.FetchCompleted(input.Data));
      return To<Loaded>();
    }

    public Type On(in Input.Fail input)
    {
      Output(new Output.FetchFailed(input.Error));
      return To<Failed>();
    }

    public Type On(in Input.Cancel input)
    {
      Output(new Output.FetchCanceled());
      return To<Canceled>();
    }
  }

  public sealed record Loaded : AsyncState;

  public sealed record Failed : AsyncState;

  public sealed record Canceled : AsyncState;
}
