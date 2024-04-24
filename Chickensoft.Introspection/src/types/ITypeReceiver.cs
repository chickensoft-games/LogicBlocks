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
