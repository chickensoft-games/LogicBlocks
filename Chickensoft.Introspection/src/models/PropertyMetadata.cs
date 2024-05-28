namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents property metadata on an introspective type.
/// </summary>
/// <param name="Name">Property name.</param>
/// <param name="IsInit">True if the property is init-only.</param>
/// <param name="Getter">Getter function.</param>
/// <param name="Setter">Setter function.</param>
/// <param name="GenericType">If the property's type is a closed constructed
/// generic type, this will be the root node of a generic node tree that
/// provides access to the individual types comprising the closed constructed
/// type.</param>
/// <param name="Attributes">Map of attribute types to attribute
/// instances.</param>
public sealed record PropertyMetadata(
  string Name,
  bool IsInit,
  Func<object, object?> Getter,
  Action<object, object?>? Setter,
  GenericType GenericType,
  Dictionary<Type, Attribute[]> Attributes
);
