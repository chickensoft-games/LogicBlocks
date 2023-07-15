namespace Chickensoft.LogicBlocks;

public abstract partial class Logic<
  TInput, TState, TOutput, THandler, TInputReturn, TUpdate
> {
  /// <summary>Logic block context provided to each logic block state.</summary>
  public readonly record struct Context {
    private Logic<
      TInput, TState, TOutput, THandler, TInputReturn, TUpdate
    > Logic { get; }

    /// <summary>
    /// Creates a new logic block context for the given logic block.
    /// </summary>
    /// <param name="logic">Logic block.</param>
    public Context(Logic<
      TInput, TState, TOutput, THandler, TInputReturn, TUpdate
    > logic) {
      Logic = logic;
    }

    /// <summary>
    /// Adds an input value to the logic block's internal input queue.
    /// </summary>
    /// <param name="input">Input to process.</param>
    /// <typeparam name="TInputType">Type of the input.</typeparam>
    /// <returns>Logic block input return value.</returns>
    public TInputReturn Input<TInputType>(TInputType input)
      where TInputType : TInput => Logic.Input(input);

    /// <summary>
    /// Produces a logic block output value.
    /// </summary>
    /// <param name="output">Output value.</param>
    public void Output(TOutput output) => Logic.OutputValue(output);

    /// <summary>
    /// Registers an entrance handler for the logic block state.
    /// </summary>
    /// <param name="handler">Callback to be invoked when the state is
    /// entered.</param>
    /// <typeparam name="TStateType">Type of state that will invoke an
    /// entrance callback.</typeparam>
    public void OnEnter<TStateType>(TUpdate handler)
      where TStateType : TState =>
        Logic.AddOnEnterCallback<TStateType>(handler);

    /// <summary>
    /// Registers an exit handler for the logic block state.
    /// </summary>
    /// <param name="handler">Callback to be invoked when the state is
    /// exited.</param>
    /// <typeparam name="TStateType">Type of state that will invoke an
    /// exit callback.</typeparam>
    public void OnExit<TStateType>(TUpdate handler)
      where TStateType : TState =>
        Logic.AddOnExitCallback<TStateType>(handler);

    /// <summary>
    /// Gets a value from the logic block's blackboard.
    /// </summary>
    /// <typeparam name="TDataType">Type of value to retrieve.</typeparam>
    /// <returns>The requested value.</returns>
    public TDataType Get<TDataType>() where TDataType : notnull =>
      Logic.Get<TDataType>();
  }
}
