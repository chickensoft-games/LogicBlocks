namespace Chickensoft.LogicBlocks;

/// <summary>
/// Utility class that enables the enter and exit callbacks on LogicBlock
/// states to be tested easily.
/// </summary>
public interface IStateTester {
  /// <summary>
  /// Runs any enter callbacks the state has registered.
  /// </summary>
  void Enter();

  /// <summary>
  /// Runs any exit callbacks the state has registered.
  /// </summary>
  void Exit();
}

internal class StateTester<TInput, TState, TOutput> : IStateTester
  where TInput : notnull
  where TState : LogicBlock<TInput, TState, TOutput>.IStateLogic
  where TOutput : notnull {
  private readonly TState _state;

  public StateTester(TState state) {
    _state = state;
  }

  /// <inheritdoc />
  public void Enter() => RunOnEnterCallbacks(_state, default!);

  /// <inheritdoc />
  public void Exit() => RunOnExitCallbacks(default!, _state);

  internal void RunOnExitCallbacks(TState state, TState previous) {
    if (
      previous is LogicBlock<TInput, TState, TOutput>.StateLogic previousLogic
    ) {
      foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
        onExit.Callback(state);
      }
    }
  }

  internal void RunOnEnterCallbacks(TState state, TState previous) {
    if (state is LogicBlock<TInput, TState, TOutput>.StateLogic stateLogic) {
      foreach (var onEnter in stateLogic.InternalState.EnterCallbacks) {
        onEnter.Callback(previous);
      }
    }
  }
}
