namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// Marks a property on a <see cref="LogicModelAttribute"/> for serialization.
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
