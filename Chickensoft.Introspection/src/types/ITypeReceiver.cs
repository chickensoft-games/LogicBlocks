namespace Chickensoft.Introspection;

/// <summary>
/// Object containing a single method that can receive a generic type argument.
/// Provided for your convenience since C# does not support generic lambdas.
/// </summary>
public interface ITypeReceiver {
  /// <summary>
  /// Generic method which receives a single generic type argument.
  /// </summary>
  /// <typeparam name="T">Generic type.</typeparam>
  void Receive<T>();
}

/// <summary>
/// Object containing a single method that can receive generic type arguments.
/// Provided for your convenience since C# does not support generic lambdas.
/// </summary>
public interface ITypeReceiver2 {
  /// <summary>
  /// Generic method which receives 2 generic type arguments.
  /// </summary>
  /// <typeparam name="TA">First generic type.</typeparam>
  /// <typeparam name="TB">Second generic type.</typeparam>
  void Receive<TA, TB>();
}
