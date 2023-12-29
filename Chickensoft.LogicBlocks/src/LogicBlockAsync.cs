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
  TState,
  Func<object, Task<TState>>,
  Task<TState>,
  Func<TState?, Task>
> where TState : class, LogicBlockAsync<TState>.IStateLogic {
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
  /// Whether or not the logic block is processing inputs.
  /// </summary>
  public override bool IsProcessing => !_processTask.Task.IsCompleted;

  private TaskCompletionSource<TState> _processTask = new();

  /// <summary>
  /// Creates a new asynchronous logic block.
  /// </summary>
  protected LogicBlockAsync() {
    _processTask.SetResult(default!);
  }

  /// <inheritdoc />
  public override TState Value => _value ?? AttachState();

  private TState AttachState() {
    _value = GetStartState();
    _value.Attach(Context);

    // If inputs were added to the logic block during the attach callbacks,
    // this will kickstart the asynchronous process of handling them. Otherwise,
    // nothing happens.
    Process();

    // Return the current state, regardless if it's about to change.
    return _value;
  }

  internal override Task<TState> Process() {
    if (_value is null) {
      // No state yet.
      AttachState();
    }

    if (IsProcessing) {
      return _processTask.Task;
    }

    ProcessInputs().ContinueWith(Continuation);

    return _processTask.Task;
  }

  private void Continuation(Task<TState> task) {
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
      await previous.Exit(state, AddError);

      previous.Detach();

      SetState(state);

      state.Attach(Context);

      // Enter the next state.
      await state.Enter(previous, AddError);

      FinalizeStateChange(state);
    }

    return Value;
  }

  /// <inheritdoc />
  public Task Start() =>
    Value.Enter(previous: null, onError: AddError);

  /// <inheritdoc />
  public Task Stop() => Value.Exit(next: null, onError: AddError);

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
