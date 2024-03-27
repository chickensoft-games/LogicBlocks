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
  /// <summary>
  /// Map of visible types that are instantiable and non-generic to a function
  /// which can be invoked to create a new instance of an object of that type.
  /// The function will throw if the type does not have a parameterless
  /// constructor.
  /// </summary>
  IDictionary<Type, Func<object>> VisibleInstantiableTypes { get; }
}
