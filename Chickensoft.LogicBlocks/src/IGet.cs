namespace Chickensoft.LogicBlocks;

/// <summary>
/// Input handler interface. Logic block states must implement this interface
/// for each type of input they wish to handle.
/// </summary>
/// <typeparam name="TInputType">Type of input to handle.</typeparam>
public interface IGet<TInputType> where TInputType : struct
{
  /// <summary>
  /// Method invoked on the state when the logic block receives an input of
  /// the corresponding type <typeparamref name="TInputType"/>.
  /// </summary>
  /// <param name="input">Input value.</param>
  /// <returns>The next state.</returns>
  Type On(in TInputType input);
}

/// <summary>
/// Input handler for a state that will hand any type of input. States that implement
/// this will only have the generic <see cref="On{TInputType}(in TInputType)" />
/// method called with the input type, rather than the specific overloads provided by
/// <see cref="IGet{TInputType}" /> (either implement <see cref="IGet{TInputType}" />
/// for each input you want the state to handle or implement this to handle any
/// input, but don't do both).
/// </summary>
public interface IGetEveryInput
{
  /// <summary>
  /// Method invoked on the state when the logic block receives an input
  /// of any type.
  /// </summary>
  /// <typeparam name="TInputType">Input type.</typeparam>
  /// <param name="input">Input value.</param>
  /// <returns>The next state.</returns>
  Type On<TInputType>(in TInputType input) where TInputType : struct;
}
