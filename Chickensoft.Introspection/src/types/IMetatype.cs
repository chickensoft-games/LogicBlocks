namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a metatype — a type that helps describe another type.
/// </summary>
public interface IMetatype {
  /// <summary>
  /// Introspective type identifier as specified by the
  /// <see cref="IdAttribute" />, if any.
  /// </summary>
  string? Id { get; }

  /// <summary>
  /// Properties on the type. Only non-partial properties marked with
  /// attributes on the current type are included. To get all of the properties,
  /// including the inherited properties from any base metatypes, see the
  /// <see cref="ITypeGraph.GetProperties(Type)"/> method.
  /// </summary>
  IReadOnlyList<PropertyMetadata> Properties { get; }

  /// <summary>Attributes applied to the type itself.</summary>
  IReadOnlyDictionary<Type, Attribute[]> Attributes { get; }

  /// <summary>
  /// List of mixins applied to the type, in the order that they were applied.
  /// </summary>
  IReadOnlyList<Type> Mixins { get; }

  /// <summary>
  /// Map of mixin handler invocation functions by mixin type.
  /// </summary>
  IReadOnlyDictionary<Type, Action<object>> MixinHandlers { get; }

  /// <summary>
  /// Calls the type receiver's <see cref="ITypeReceiver.Receive{T}"/> method
  /// with the generic type as the argument.
  /// </summary>
  /// <param name="receiver">Generic type argument receiver.</param>
  void GetGenericType(ITypeReceiver receiver);
}
