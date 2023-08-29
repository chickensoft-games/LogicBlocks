namespace Chickensoft.LogicBlocks;

using System.Threading.Tasks;

/// <summary>
/// Utility class that enables the asynchronous enter and exit callbacks on
/// LogicBlock states to be tested easily.
/// </summary>
public interface IStateTesterAsync {
  /// <summary>
  /// Runs any enter callbacks the state has registered.
  /// </summary>
  Task Enter();

  /// <summary>
  /// Runs any exit callbacks the state has registered.
  /// </summary>
  Task Exit();
}

internal class StateTesterAsync<TInput, TState, TOutput> : IStateTesterAsync
  where TInput : notnull
  where TState : LogicBlockAsync<TInput, TState, TOutput>.IStateLogic
  where TOutput : notnull {
  private readonly TState _state;

  public StateTesterAsync(TState state) {
    _state = state;
  }

  /// <inheritdoc />
  public async Task Enter() => await RunOnEnterCallbacks(_state, default!);

  /// <inheritdoc />
  public async Task Exit() => await RunOnExitCallbacks(default!, _state);

  private async Task RunOnExitCallbacks(TState state, TState previous) {
    if (
      previous is LogicBlockAsync<
        TInput, TState, TOutput
      >.StateLogic previousLogic
    ) {
      foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
        await onExit.Callback(state);
      }
    }
  }

  private async Task RunOnEnterCallbacks(TState state, TState previous) {
    if (
      state is LogicBlockAsync<TInput, TState, TOutput>.StateLogic stateLogic
    ) {
      foreach (var onEnter in stateLogic.InternalState.EnterCallbacks) {
        await onEnter.Callback(previous);
      }
    }
  }
}
