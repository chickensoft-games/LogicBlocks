namespace Chickensoft.LogicBlocks;

public abstract partial class LogicBlock<TInput, TState, TOutput> {
  /// <summary>
  /// Input handler interface. Logic block states must implement this interface
  /// for each type of input they wish to handle.
  /// </summary>
  /// <typeparam name="TInputType">Type of input to handle.</typeparam>
  public interface IGet<TInputType> where TInputType : TInput {
    /// <summary>
    /// Method invoked on the state when the logic block receives an input of
    /// the corresponding type <typeparamref name="TInputType"/>.
    /// </summary>
    /// <param name="input">Input value.</param>
    /// <returns>The next state of the logic block.</returns>
    TState On(TInputType input);
  }
}
