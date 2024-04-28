namespace Chickensoft.Serialization;

using System;

/// <summary>
/// Indicates that a property should be serialized.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SaveAttribute : Attribute {
  /// <summary>
  /// Name to use for the property when serializing and deserializing.
  /// </summary>
  public string Id { get; }

  /// <summary>
  /// Creates a new instance of the <see cref="SaveAttribute"/> class
  /// with the specified property identifier. This attribute marks a property
  /// for serialization.
  /// </summary>
  /// <param name="id">Name to use for the property when serializing and
  /// deserializing.</param>
  public SaveAttribute(string id) {
    Id = id;
  }
}
