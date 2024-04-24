namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a metatype — a type that helps describe another type.
/// </summary>
public interface IMetatype {
  /// <summary>
  /// Metatype identifier. This will be used as the type discriminator for
  /// serialization and deserialization if used with LogicBlock's serialization
  /// utilities.
  /// </summary>
  string Id { get; }

  /// <summary>
  /// Properties on the type. Only non-partial properties marked with
  /// attributes on the current type are included. To get all of the properties,
  /// including the inherited properties from any base metatypes, see the
  /// <see cref="Types.GetAllProperties"/> method.
  /// </summary>
  IList<PropertyMetadata> Properties { get; }

  /// <summary>Attributes applied to the type itself.</summary>
  IDictionary<Type, Attribute[]> Attributes { get; }

  /// <summary>
  /// List of mixins applied to the type, in the order that they were applied.
  /// </summary>
  IList<Type> Mixins { get; }

  /// <summary>
  /// Map of mixin handler invocation functions by mixin type.
  /// </summary>
  IDictionary<Type, Action<object>> MixinHandlers { get; }
}
