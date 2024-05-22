namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a registry of types containing all the types that are visible
/// at the top-level of the application.
/// </summary>
public interface ITypeRegistry {
  /// <summary>
  /// Map of system types to types that are visible from the top-level
  /// assemblies of the code that the type generator has been executed on.
  /// </summary>
  IReadOnlyDictionary<Type, ITypeMetadata> VisibleTypes { get; }
}
