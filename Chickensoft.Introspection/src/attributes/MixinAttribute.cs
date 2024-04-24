namespace Chickensoft.Introspection;

using System;

/// <summary>
/// Mixin attribute. A mixin is an interface that extends
/// <see cref="IMixin{TMixin}"/>
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class MixinAttribute : Attribute {
  /// <summary
  /// >Defines a mixin. A mixin is an interface that extends
  /// <see cref="IMixin{TMixin}"/>
  /// </summary>
  public MixinAttribute() { }
}
