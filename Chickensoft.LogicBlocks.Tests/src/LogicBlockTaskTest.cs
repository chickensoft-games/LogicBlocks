namespace Chickensoft.LogicBlocks.Tests;

using Fixtures;
using Shouldly;

public class LogicBlockTaskTest
{
  [Fact]
  public async Task InputDeliversInputOnSuccess()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();

    new StatefulTask<string>(tcs.Task, tester)
      .Input(val => new TestLogicBlockState.Input.TestInput());

    tcs.SetResult("succeeded");
    await tester.Task;

    tester.Inputs.ShouldContain(new TestLogicBlockState.Input.TestInput());
  }

  [Fact]
  public async Task ErrorInputDeliversInputOnFault()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();

    new StatefulTask<string>(tcs.Task, tester)
      .ErrorInput(ex => new TestLogicBlockState.Input.TestInput());

    tcs.SetException(new InvalidOperationException("error"));
    await tester.Task;

    tester.Inputs.ShouldContain(new TestLogicBlockState.Input.TestInput());
  }

  [Fact]
  public async Task ErrorInputReceivesAggregateException()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();
    Exception? received = null;

    new StatefulTask<string>(tcs.Task, tester)
      .ErrorInput(ex =>
      {
        received = ex;
        return new TestLogicBlockState.Input.TestInput();
      });

    var inner = new InvalidOperationException("inner exception");
    tcs.SetException(inner);
    await tester.Task;

    var aggregate = received.ShouldBeOfType<AggregateException>();
    aggregate.InnerException.ShouldBe(inner);
  }

  [Fact]
  public async Task CanceledInputDeliversInputOnCancellation()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();

    new StatefulTask<string>(tcs.Task, tester)
      .CanceledInput(() => new TestLogicBlockState.Input.TestInput());

    tcs.SetCanceled();
    await tester.Task;

    tester.Inputs.ShouldContain(new TestLogicBlockState.Input.TestInput());
  }

  [Fact]
  public async Task UsesCurrentSchedulerWhenNoSynchronizationContext()
  {
    var previous = SynchronizationContext.Current;
    try
    {
      SynchronizationContext.SetSynchronizationContext(null);

      var state = new TestLogicBlockState();
      var tester = state.Test();
      var tcs = new TaskCompletionSource<string>();

      new StatefulTask<string>(tcs.Task, tester)
        .Input(val => new TestLogicBlockState.Input.TestInput());

      tcs.SetResult("succeeded");
      await tester.Task;

      tester.Inputs.ShouldContain(new TestLogicBlockState.Input.TestInput());
    }
    finally
    {
      SynchronizationContext.SetSynchronizationContext(previous);
    }
  }

  [Fact]
  public async Task ChainingAllThreeOnlyFiresMatchingHandler()
  {
    var state = new TestLogicBlockState();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();
    var errorFired = false;
    var canceledFired = false;

    new StatefulTask<string>(tcs.Task, tester)
      .Input(val => new TestLogicBlockState.Input.TestInput())
      .ErrorInput(ex =>
      {
        errorFired = true;
        return new TestLogicBlockState.Input.TestInput();
      })
      .CanceledInput(() =>
      {
        canceledFired = true;
        return new TestLogicBlockState.Input.TestInput();
      });

    tcs.SetResult("succeeded");
    await tester.Task;

    tester.Inputs.Count.ShouldBe(1);
    errorFired.ShouldBeFalse();
    canceledFired.ShouldBeFalse();
  }

}
