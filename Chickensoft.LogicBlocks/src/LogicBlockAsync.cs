namespace Chickensoft.LogicBlocks;

using System;
using System.Threading.Tasks;

/// <summary>
/// <para>
/// A synchronous logic block. Logic blocks are machines that process inputs
/// one-at-a-time, maintain a current state graph, and produce outputs.
/// </para>
/// <para>
/// Logic blocks are essentially statecharts that are created using the state
/// pattern. Each state is a self-contained class, record, or struct that
/// implements <see cref="Logic{TInput, TState, TOutput, THandler,
/// TInputReturn, TUpdate}.IStateLogic"/>.
/// </para>
/// </summary>
/// <typeparam name="TInput">Input type.</typeparam>
/// <typeparam name="TState">State type.</typeparam>
/// <typeparam name="TOutput">Output type.</typeparam>
public interface ILogicBlockAsync<TInput, TState, TOutput>
  : ILogic<
      TInput,
      TState,
      TOutput,
      Func<TInput, Task<TState>>,
      Task<TState>, Func<TState, Task>
    >
  where TInput : notnull
  where TState : Logic<
    TInput,
    TState,
    TOutput,
    Func<TInput, Task<TState>>,
    Task<TState>,
    Func<TState, Task>
  >.IStateLogic
  where TOutput : notnull {
  /// <summary>
  /// Returns the initial state of the logic block. Implementations must
  /// override this method to provide a valid initial state.
  /// </summary>
  /// <param name="context">Logic block context.</param>
  /// <returns>Initial state of the logic block.</returns>
  TState GetInitialState(
    Logic<
      TInput,
      TState,
      TOutput,
      Func<TInput, Task<TState>>,
      Task<TState>,
      Func<TState, Task>
    >.IContext context
  );

  /// <summary>
  /// Calls the entrance callbacks for the initial state. Technically, you can
  /// call this whenever you'd like to re-trigger entrance callbacks for the
  /// current state, but it's not recommended.
  /// <br />
  /// Calling this is optional. LogicBlocks doesn't invoke the initial state's
  /// entrance callbacks by default since the <see cref="Logic{
  ///   TInput, TState, TOutput, THandler, TInputReturn, TUpdate
  /// }.Value"/> property  must be synchronous and the side effects of the
  /// initial state are often redundant in typical use cases representing the
  /// default starting state.
  /// <br />
  /// This is provided for those times when you actually do need the side
  /// effects from the initial state's entrance callbacks.
  /// </summary>
  /// <returns>Asynchronous task that finishes when the OnEntrance callbacks
  /// for the current state complete.</returns>
  Task Start();
}

/// <summary>
/// <para>
/// A synchronous logic block. Logic blocks are machines that process inputs
/// one-at-a-time, maintain a current state graph, and produce outputs.
/// </para>
/// <para>
/// Logic blocks are essentially statecharts that are created using the state
/// pattern. Each state is a self-contained class, record, or struct that
/// implements <see cref="Logic{
///   TInput, TState, TOutput, THandler, TInputReturn, TUpdate
/// }.IStateLogic"/>.
/// </para>
/// </summary>
/// <typeparam name="TInput">Input type.</typeparam>
/// <typeparam name="TState">State type.</typeparam>
/// <typeparam name="TOutput">Output type.</typeparam>
public abstract partial class LogicBlockAsync<TInput, TState, TOutput> :
  Logic<
    TInput,
    TState,
    TOutput,
    Func<TInput, Task<TState>>,
    Task<TState>,
    Func<TState, Task>
  >,
  ILogicBlockAsync<TInput, TState, TOutput>
  where TInput : notnull
  where TState : Logic<
    TInput,
    TState,
    TOutput,
    Func<TInput, Task<TState>>,
    Task<TState>,
    Func<TState, Task>
  >.IStateLogic
  where TOutput : notnull {
  /// <summary>
  /// Creates a new utility for testing the enter and exit callbacks
  /// asynchronously on a state.
  /// </summary>
  /// <param name="state">State to be tested.</param>
  /// <returns>A new state tester.</returns>
  public static IStateTesterAsync Test(TState state) =>
    new StateTesterAsync<TInput, TState, TOutput>(state);

  /// <summary>
  /// The context provided to the states of the logic block.
  /// </summary>
  public new IContext Context { get; }

  /// <summary>
  /// Whether or not the logic block is processing inputs.
  /// </summary>
  public override bool IsProcessing => !_processTask.Task.IsCompleted;

  private TaskCompletionSource<TState> _processTask = new();

  /// <summary>
  /// Creates a new asynchronous logic block.
  /// </summary>
  protected LogicBlockAsync() {
    Context = new Context(this);
    _processTask.SetResult(default!);
  }

  /// <inheritdoc />
  public sealed override TState GetInitialState() => GetInitialState(Context);

  /// <inheritdoc />
  public abstract TState GetInitialState(IContext context);

  /// <inheritdoc />
  public async Task Start() {
    if (IsProcessing) {
      return;
    }

    await CallOnEnterCallbacks(default!, Value);
  }

  internal override Task<TState> Process() {
    if (IsProcessing) {
      return _processTask.Task;
    }

    ProcessInputs().ContinueWith((task) => {
      if (task.IsFaulted) {
        // Logic blocks are designed to catch all errors in state changes,
        // so if this happens it means the logic block overrode HandleError
        // and re-threw the exception.
        //
        // In that case, we want to respect the decision to stop execution and
        // end the task with the exception.
        _processTask.SetException(task.Exception!);
        return;
      }
      _processTask.SetResult(Value);
    });

    return _processTask.Task;
  }

  private async Task<TState> ProcessInputs() {
    _processTask = new();

    while (GetNextInput(out var pendingInput)) {
      var handler = pendingInput.GetHandler();
      var input = pendingInput.Input;

      // Get next state.
      var state = await handler(input);

      AnnounceInput(input);

      if (!CanChangeState(state)) {
        // The only time we can't change states is if the new state is
        // equivalent to the old state (determined by the default equality
        // comparer)
        continue;
      }

      var previous = Value;

      // Call OnExit callbacks for StateLogic states.
      await CallOnExitCallbacks(previous, state);

      SetState(state);

      // Call OnEnter callbacks for StateLogic states.
      await CallOnEnterCallbacks(previous, state);

      FinalizeStateChange(state);
    }

    return Value;
  }

  internal async Task CallOnEnterCallbacks(TState previous, TState next) {
    if (next is StateLogic nextStateLogic) {
      foreach (var onEnter in nextStateLogic.InternalState.EnterCallbacks) {
        if (onEnter.IsType(previous)) {
          // Already entered this state type.
          continue;
        }
        await RunSafe(() => onEnter.Callback(previous));
      }
    }
  }

  internal async Task CallOnExitCallbacks(TState previous, TState next) {
    if (previous is StateLogic previousLogic) {
      foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
        if (onExit.IsType(next)) {
          // Not actually leaving this state type.
          continue;
        }
        await RunSafe(() => onExit.Callback(next));
      }
    }
  }

  internal override Func<TInput, Task<TState>> GetInputHandler<TInputType>()
    => (input) => {
      if (Value is IGet<TInputType> stateWithHandler) {
        return RunSafe(() => stateWithHandler.On((TInputType)input), Value);
      }

      return Task.FromResult(Value);
    };

  private async Task<TState> RunSafe(
    Func<Task<TState>> handler, TState fallback
  ) {
    try { return await handler(); }
    catch (Exception e) { AddError(e); }
    return fallback;
  }

  private async Task RunSafe(Func<Task> callback) {
    try { await callback(); }
    catch (Exception e) { AddError(e); }
  }
}
