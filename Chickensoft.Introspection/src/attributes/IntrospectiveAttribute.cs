namespace Chickensoft.Introspection;

using System;

/// <summary>
/// Indicates that a reference type is an introspective type. The type should
/// be a partial class or partial record class. Metadata about the type
/// (referred to as a metatype) will be generated at compile-time by the
/// introspection generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IntrospectiveAttribute : Attribute {
  /// <summary>
  /// The identifier of the type. All metatypes require an identifier for
  /// easy lookups. The identifier should be unique across all assemblies used
  /// in your project.
  /// </summary>
  public string? Id { get; }

  /// <summary>
  /// Mixins applied to the type. Mixins are interfaces that are applied to the
  /// type at build-time, allowing them to be looked up later.
  /// </summary>
  public Type[] Mixins { get; }

  /// <summary>
  /// <inheritdoc cref="IntrospectiveAttribute" path="/summary"/>
  /// </summary>
  /// <param name="id"><inheritdoc cref="Id" path="/summary"/></param>
  /// <param name="mixins"><inheritdoc cref="Mixins" path="/summary"/></param>
  public IntrospectiveAttribute(string id, params Type[] mixins) {
    Id = id;
    Mixins = mixins;
  }

  /// <summary>
  /// <inheritdoc cref="IntrospectiveAttribute" path="/summary"/>
  /// </summary>
  /// <param name="mixins"><inheritdoc cref="Mixins" path="/summary"/></param>
  public IntrospectiveAttribute(params Type[] mixins) {
    Id = null;
    Mixins = mixins;
  }
}
