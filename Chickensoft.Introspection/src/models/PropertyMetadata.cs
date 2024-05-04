namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents property metadata on an introspective type.
/// </summary>
/// <param name="Name">Property name.</param>
/// <param name="Type">Property type.</param>
/// <param name="Getter">Getter function.</param>
/// <param name="Setter">Setter function.</param>
/// <param name="GenericTypeGetter">Function which invokes the provided
/// <see cref="ITypeReceiver" /> with the generic type of the property.</param>
/// <param name="AttributesByType">Map of attribute types to attribute
/// instances.</param>
public sealed record PropertyMetadata(
  string Name,
  Type Type,
  Func<object, object?> Getter,
  Action<object, object?>? Setter,
  Action<ITypeReceiver> GenericTypeGetter,
  Dictionary<Type, Attribute[]> AttributesByType
);