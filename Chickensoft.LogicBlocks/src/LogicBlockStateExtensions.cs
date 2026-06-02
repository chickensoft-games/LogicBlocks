namespace Chickensoft.LogicBlocks;

public static class StateExtensions
{
  public static void OnEnter<TState>(this TState state, Action callback)
    where TState : LogicBlockState =>
    state.InternalState.AddOnEnterCallback<TState>(callback);

  public static void OnExit<TState>(this TState state, Action callback)
    where TState : LogicBlockState =>
    state.InternalState.AddOnExitCallback<TState>(callback);
}
