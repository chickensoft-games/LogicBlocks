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
/// pattern. Each state is a self-contained record.
/// </para>
/// </summary>
/// <typeparam name="TState">State type.</typeparam>
public interface ILogicBlockAsync<TState> : ILogic<
  TState, Func<object, Task<TState>>, Task<TState>, Func<TState?, Task>
> where TState : class, LogicBlockAsync<TState>.IStateLogic {
  /// <summary>
  /// Returns the initial state of the logic block. Implementations must
  /// override this method to provide a valid initial state.
  /// </summary>
  /// <param name="context">Logic block context.</param>
  /// <returns>Initial state of the logic block.</returns>
  TState GetInitialState(
    Logic<
      TState,
      Func<object, Task<TState>>,
      Task<TState>,
      Func<TState?, Task>
    >.IContext context
  );

  /// <summary>
  /// Starts the logic block by entering the current state. If the logic block
  /// hasn't initialized yet, this will create the initial state before entering
  /// it.
  /// </summary>
  Task Start();

  /// <summary>
  /// Stops the logic block by exiting the current state. For best results,
  /// don't continue to give inputs to the logic block after stopping it.
  /// Otherwise, you might end up firing the exit handler of a state more than
  /// once.
  /// </summary>
  Task Stop();
}

/// <summary>
/// <para>
/// A synchronous logic block. Logic blocks are machines that process inputs
/// one-at-a-time, maintain a current state graph, and produce outputs.
/// </para>
/// <para>
/// Logic blocks are essentially statecharts that are created using the state
/// pattern. Each state is a self-contained record.
/// </para>
/// </summary>
/// <typeparam name="TState">State type.</typeparam>
public abstract partial class LogicBlockAsync<TState> : Logic<
  TState,
  Func<object, Task<TState>>,
  Task<TState>,
  Func<TState?, Task>
>,
ILogicBlockAsync<
  TState
> where TState : class, LogicBlockAsync<TState>.IStateLogic {
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

      // Exit the previous state.
      await previous.Exit(state, (e) => AddError(e));

      SetState(state);

      // Enter the next state.
      await state.Enter(previous, (e) => AddError(e));

      FinalizeStateChange(state);
    }

    return Value;
  }

  /// <inheritdoc />
  public Task Start() =>
    Value.Enter(previous: null, onError: (e) => AddError(e));

  /// <inheritdoc />
  public Task Stop() => Value.Exit(next: null, onError: (e) => AddError(e));

  internal override Func<object, Task<TState>> GetInputHandler<TInputType>()
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
    catch (Exception e) {
      AddError(e);
    }
    return fallback;
  }
}
