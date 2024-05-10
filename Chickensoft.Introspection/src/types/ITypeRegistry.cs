namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a registry of types containing all the types that are visible
/// at the top-level of the application.
/// </summary>
public interface ITypeRegistry {
  /// <summary>
  /// Map of non-generic reference types that are visible from the assembly's
  /// global scope to their names.
  /// </summary>
  IReadOnlyDictionary<Type, string> VisibleTypes { get; }
  /// <summary>
  /// Map of non-generic, non-abstract, reference types that are visible from
  /// the assembly's global scope.
  /// </summary>
  IReadOnlyDictionary<Type, TypeMetadata> ConcreteVisibleTypes { get; }

  /// <summary>
  /// Map of metatype instances by the system type of their associated
  /// introspective type.
  /// </summary>
  IReadOnlyDictionary<Type, IMetatype> Metatypes { get; }
}
