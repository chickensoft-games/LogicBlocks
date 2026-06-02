namespace Chickensoft.LogicBlocks.Tests;

using Fixtures;
using Shouldly;

public class AsyncStateUnitTest
{
  [Fact]
  public void FetchTransitionsToLoading()
  {
    var state = new AsyncState.Idle();
    _ = state.Test();
    var tcs = new TaskCompletionSource<string>();

    var result = state.On(new AsyncState.Input.Fetch(tcs.Task));

    result.ShouldBe(typeof(AsyncState.Loading));
  }

  [Fact]
  public void FetchEmitsFetchStartedOutput()
  {
    var state = new AsyncState.Idle();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();

    state.On(new AsyncState.Input.Fetch(tcs.Task));

    tester.Outputs.ShouldContain(new AsyncState.Output.FetchStarted());
  }

  [Fact]
  public async Task AsyncSuccessDeliversSucceedInput()
  {
    var state = new AsyncState.Idle();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();

    state.On(new AsyncState.Input.Fetch(tcs.Task));

    tcs.SetResult("succeeded");
    await tester.Task;

    tester.Inputs.ShouldContain(new AsyncState.Input.Succeed("succeeded"));
  }

  [Fact]
  public async Task AsyncFaultDeliversFailInput()
  {
    var state = new AsyncState.Idle();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();

    state.On(new AsyncState.Input.Fetch(tcs.Task));

    tcs.SetException(new InvalidOperationException("error"));
    await tester.Task;

    tester.Inputs.Count.ShouldBe(1);
    var failInput = tester.Inputs[0].ShouldBeOfType<AsyncState.Input.Fail>();
    failInput.Error.ShouldBeOfType<AggregateException>();
  }

  [Fact]
  public async Task AsyncCancelDeliversCancelInput()
  {
    var state = new AsyncState.Idle();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();

    state.On(new AsyncState.Input.Fetch(tcs.Task));

    tcs.SetCanceled();
    await tester.Task;

    tester.Inputs.ShouldContain(new AsyncState.Input.Cancel());
  }

  [Fact]
  public async Task OnlyMatchingHandlerFires()
  {
    var state = new AsyncState.Idle();
    var tester = state.Test();
    var tcs = new TaskCompletionSource<string>();

    state.On(new AsyncState.Input.Fetch(tcs.Task));

    tcs.SetResult("succeeded");
    await tester.Task;

    tester.Inputs.Count.ShouldBe(1);
    tester.Inputs[0].ShouldBeOfType<AsyncState.Input.Succeed>();
  }

  [Fact]
  public void LoadingOnSucceedTransitionsToLoaded()
  {
    var state = new AsyncState.Loading();
    var tester = state.Test();

    var result = state.On(new AsyncState.Input.Succeed("succeeded"));

    result.ShouldBe(typeof(AsyncState.Loaded));
    tester.Outputs.ShouldContain(
      new AsyncState.Output.FetchCompleted("succeeded")
    );
  }

  [Fact]
  public void LoadingOnFailTransitionsToFailed()
  {
    var state = new AsyncState.Loading();
    var tester = state.Test();
    var ex = new InvalidOperationException("error");

    var result = state.On(new AsyncState.Input.Fail(ex));

    result.ShouldBe(typeof(AsyncState.Failed));
    tester.Outputs.Count.ShouldBe(1);
    var output = tester.Outputs[0]
      .ShouldBeOfType<AsyncState.Output.FetchFailed>();
    output.Error.ShouldBe(ex);
  }

  [Fact]
  public void LoadingOnCancelTransitionsToCanceled()
  {
    var state = new AsyncState.Loading();
    var tester = state.Test();

    var result = state.On(new AsyncState.Input.Cancel());

    result.ShouldBe(typeof(AsyncState.Canceled));
    tester.Outputs.ShouldContain(new AsyncState.Output.FetchCanceled());
  }
}

public class AsyncLogicIntegrationTest
{
  [Fact]
  public async Task EndToEndSuccess()
  {
    using var logic = new AsyncLogic();
    var tcs = new TaskCompletionSource<string>();

    logic.Start<AsyncState.Idle>();
    logic.Input(new AsyncState.Input.Fetch(tcs.Task));
    logic.State.ShouldBeOfType<AsyncState.Loading>();

    tcs.SetResult("payload");
    await logic.Task;

    logic.State.ShouldBeOfType<AsyncState.Loaded>();
  }

  [Fact]
  public async Task EndToEndFault()
  {
    using var logic = new AsyncLogic();
    var tcs = new TaskCompletionSource<string>();

    logic.Start<AsyncState.Idle>();
    logic.Input(new AsyncState.Input.Fetch(tcs.Task));

    tcs.SetException(new InvalidOperationException("error"));
    await logic.Task;

    logic.State.ShouldBeOfType<AsyncState.Failed>();
  }

  [Fact]
  public async Task EndToEndCancel()
  {
    using var logic = new AsyncLogic();
    var tcs = new TaskCompletionSource<string>();

    logic.Start<AsyncState.Idle>();
    logic.Input(new AsyncState.Input.Fetch(tcs.Task));

    tcs.SetCanceled();
    await logic.Task;

    logic.State.ShouldBeOfType<AsyncState.Canceled>();
  }

  [Fact]
  public async Task StatusGuardPreventsInputAfterStop()
  {
    using var logic = new AsyncLogic();
    var tcs = new TaskCompletionSource<string>();

    logic.Start<AsyncState.Idle>();
    logic.Input(new AsyncState.Input.Fetch(tcs.Task));
    logic.State.ShouldBeOfType<AsyncState.Loading>();

    logic.Stop();

    tcs.SetResult("too late");
    await logic.Task;

    logic.State.ShouldBeNull();
    logic.Status.ShouldBe(LogicBlockStatus.Stopped);
  }

  [Fact]
  public async Task StatusGuardPreventsInputAfterDispose()
  {
    var logic = new AsyncLogic();
    var tcs = new TaskCompletionSource<string>();

    logic.Start<AsyncState.Idle>();
    logic.Input(new AsyncState.Input.Fetch(tcs.Task));

    logic.Dispose();

    tcs.SetResult("too late");
    await logic.Task;

    logic.State.ShouldBeNull();
    logic.Status.ShouldBe(LogicBlockStatus.Disposed);
  }
}
