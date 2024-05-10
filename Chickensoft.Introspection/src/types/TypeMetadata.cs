namespace Chickensoft.Introspection;

using System;

/// <summary>
/// Represents a visible type identified in a codebase.
/// </summary>
/// <param name="Name">The simple name of the type.</param>
/// <param name="GenericTypeGetter">Function which receives a type receiver
/// and calls the <see cref="ITypeReceiver.Receive{T}" /> method with the
/// generic type.</param>
/// <param name="Factory">Function which returns a new instance of the type.
/// </param>
public record TypeMetadata(
  string Name,
  Action<ITypeReceiver> GenericTypeGetter,
  Func<object> Factory
);
