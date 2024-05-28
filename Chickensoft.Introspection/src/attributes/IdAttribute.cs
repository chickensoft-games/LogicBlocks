namespace Chickensoft.Introspection;

using System;

/// <summary>
/// Identifies an introspective type with a unique identifier string. This
/// identifier string will be output by the introspection generator on the
/// type's associated metatype information. The identifier should be unique
/// across all introspective types from every assembly used in your project.
/// </summary>
[AttributeUsage(
  AttributeTargets.Class,
  AllowMultiple = false,
  Inherited = true
)]
public class IdAttribute : Attribute {
  /// <summary>
  /// Unique string identifier for an introspective type. Must be unique across
  /// all introspective types used across every assembly in your project.
  /// </summary>
  public string Id { get; }

  /// <inheritdoc cref="IdAttribute" path="/summary"/>
  /// <param name="id"><inheritdoc cref="Id" path="/summary"/></param>
  public IdAttribute(string id) {
    Id = id;
  }
}
