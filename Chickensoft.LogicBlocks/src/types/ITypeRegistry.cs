namespace Chickensoft.LogicBlocks.Types;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a registry of types containing all the types that are visible
/// at the top-level of the application.
/// </summary>
public interface ITypeRegistry {
  /// <summary>
  /// Visible types, including any visible abstract types and interfaces.
  /// </summary>
  ISet<Type> VisibleTypes { get; }
  /// <summary>Only visible types that are also instantiable.</summary>
  ISet<Type> VisibleInstantiableTypes { get; }
}
