namespace Chickensoft.Introspection;

using System;

/// <summary>
/// Indicates that a reference type is an introspective type. The type should
/// be a partial class or partial record class. Metadata about the type
/// (referred to as a metatype) will be generated at compile-time by the
/// introspection generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class MetaAttribute : Attribute {

  /// <summary>
  /// Mixins applied to the type. Mixins are interfaces that extend
  /// <see cref="IMixin{TMixin}" /> with a single handler method that are
  /// applied to the type at build-time. Mixins and their handlers can be
  /// looked up by the type itself at runtime, allowing mixins to be
  /// dynamically invoked without the type having to know what mixins are
  /// applied to it.
  /// </summary>
  public Type[] Mixins { get; }

  /// <summary>
  /// <inheritdoc cref="MetaAttribute" path="/summary"/>
  /// </summary>
  /// <param name="mixins"><inheritdoc cref="Mixins" path="/summary"/></param>
  public MetaAttribute(params Type[] mixins) {
    Mixins = mixins;
  }
}
