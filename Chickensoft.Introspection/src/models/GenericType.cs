namespace Chickensoft.Introspection;

using System;

/// <summary>
/// Generic type representation.
/// </summary>
/// <param name="OpenType">Open generic type.</param>
/// <param name="ClosedType">Closed generic type.</param>
/// <param name="Arguments">Type arguments.</param>
/// <param name="GenericTypeGetter">Action which invokes the generic type
/// receiver with the generic type.</param>
/// <param name="GenericTypeGetter2">Action which invokes the generic type
/// receiver with its two child generic types.</param>
public record GenericType(
  Type OpenType,
  Type ClosedType,
  GenericType[] Arguments,
  Action<ITypeReceiver> GenericTypeGetter,
  Action<ITypeReceiver2>? GenericTypeGetter2
);
