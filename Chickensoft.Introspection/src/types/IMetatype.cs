namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a metatype — a type that helps describe another type.
/// </summary>
public interface IMetatype {
  /// <summary>System type of the introspective type.</summary>
  Type Type { get; }

  /// <summary>
  /// True if the type has init-only properties that must be set at
  /// construction. If this is true for a concrete type, you may call
  /// <see cref="Construct"/> with a map of argument names to values to set
  /// these properties at construction.
  /// </summary>
  bool HasInitProperties { get; }

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
  /// Constructs the type with the given arguments, if any. If the type is not
  /// a concrete type, this throws. If the type has init-only properties, this
  /// can be used to set them at construction.
  /// </summary>
  /// <param name="args">Map of argument names to values.</param>
  /// <returns>A new instance of the type.</returns>
  object Construct(IReadOnlyDictionary<string, object?>? args = null);
}
