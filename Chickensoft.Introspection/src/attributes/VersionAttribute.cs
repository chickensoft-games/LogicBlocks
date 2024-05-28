namespace Chickensoft.Introspection;

using System;

/// <summary>
/// Indicates the version of an introspective type.
/// </summary>
[AttributeUsage(
  AttributeTargets.Class,
  AllowMultiple = false,
  Inherited = true
)]
public class VersionAttribute : Attribute {
  /// <summary>
  /// Version of the introspective type. This is a simple integer that is >= 1.
  /// </summary>
  public int Version { get; }

  /// <inheritdoc cref="VersionAttribute" path="/summary"/>
  /// <param name="version"><inheritdoc cref="Version" path="/summary"/></param>
  public VersionAttribute(int version) {
    Version = Math.Max(version, 1);
  }
}
